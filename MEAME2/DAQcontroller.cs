using System;
using System.Collections.Generic;

using Mcs.Usb;

namespace MEAME2
{

  public class DAQ {

    public int samplerate { get; set; }
    public int segmentLength { get; set; }
    static int mChannelHandles { get; set; }
    static int hwchannels { get; set; }
    public SampleSizeNet dataFormat { get; set; }
    public Action<Dictionary<int, int[]>, int> onChannelData { get; set; }

    private int someCounter { get; set; }

    private Random rnd { get; set; }

    public override String ToString(){
      return deviceInfo;
    }

    private readonly CMcsUsbListNet usblist = new CMcsUsbListNet();
    private CMeaDeviceNet dataAcquisitionDevice;
    private string deviceInfo = "Uninitialized DACQ device";

    // To say I hate writing code like this is an understatement
    public bool startDevice(){
      try { dataAcquisitionDevice.StartDacq(); return true; }
      catch (Exception e) { return false; }
    }

    public bool stopDevice(){
      try { dataAcquisitionDevice.StopDacq(); return true; }
      catch (Exception e) { return false; }
    }


    public bool connectDataAcquisitionDevice(uint index){

      this.rnd = new Random();
      this.someCounter = 0;

      this.dataFormat = SampleSizeNet.SampleSize32Signed;

      dataAcquisitionDevice = new CMeaDeviceNet(usblist.GetUsbListEntry(index).DeviceId.BusType,
                                                _onChannelData,
                                                onError);

      // The second arg refers to lock mask, allowing multiple device objects to be connected
      // to the same physical device. Yes, I know, what the fuck...
      dataAcquisitionDevice.Connect(usblist.GetUsbListEntry(index), 1);
      dataAcquisitionDevice.SendStop();

      int what = 0;
      dataAcquisitionDevice.HWInfo().GetNumberOfHWADCChannels(out what);
      hwchannels = what;

      dataAcquisitionDevice.SetNumberOfChannels(hwchannels);
      dataAcquisitionDevice.EnableDigitalIn(false, 0);
      dataAcquisitionDevice.EnableChecksum(false, 0);
      dataAcquisitionDevice.SetDataMode(DataModeEnumNet.dmSigned32bit, 0);

      // block:
      // get the number of 16 bit datawords which will be collected per sample frame,
      // use after the device is configured. (which means?, setting data mode, num channels etc?)
      int ana, digi, che, tim, block;
      dataAcquisitionDevice.GetChannelLayout(out ana, out digi, out che, out tim, out block, 0);

      dataAcquisitionDevice.SetSampleRate(samplerate, 1, 0);

      int gain = dataAcquisitionDevice.GetGain();

      List<CMcsUsbDacqNet.CHWInfo.CVoltageRangeInfoNet> voltageranges;
      dataAcquisitionDevice.HWInfo().
        GetAvailableVoltageRangesInMicroVoltAndStringsInMilliVolt(out voltageranges);


      bool[] selectedChannels = new bool[block/2];
      for (int i = 0; i < block/2; i++){ selectedChannels[i] = true; } // hurr


      bool[] nChannels         = selectedChannels;
      int queueSize            = 240000;
      int threshold            = segmentLength;
      SampleSizeNet sampleSize = dataFormat;           // Signed32
      int ChannelsInBlock      = block/2;              // 64

      dataAcquisitionDevice.SetSelectedChannelsQueue
        (nChannels,
         queueSize,
         threshold,
         sampleSize,
         ChannelsInBlock);

      mChannelHandles = block;

      dataAcquisitionDevice.ChannelBlock_SetCheckChecksum((uint)che, (uint)tim); // ???

      // int voltrange = voltageranges.ToArray()[0];

      int validDataBits = -1;
      int deviceDataFormat = -1;

      /**
      Summary:
          Get the real number of data bits.

      Remarks:
          This value may be different from the value returned by GetDataFormat, e.g. in
          MC_Card the data are shifted 2 bits so the real number is 14 while the data format
          is 16 bits
      */
      dataAcquisitionDevice.GetNumberOfDataBits(0,
                                                DacqGroupChannelEnumNet.HeadstageElectrodeGroup,
                                                out validDataBits);

      dataAcquisitionDevice.GetDataFormat(0,
                                          DacqGroupChannelEnumNet.HeadstageElectrodeGroup,
                                          out deviceDataFormat);

      DataModeEnumNet dataMode = dataAcquisitionDevice.GetDataMode(0);


      deviceInfo =
        "Data acquisition device connected to physical device with parameters: \n" +
        $"[SetSelectedChannelsQueue arguments:]\n" +
        $"nChannels           \t{selectedChannels}\n" +
        $"queueSize:          \t{queueSize}\n" +
        $"threshold:          \t{threshold}\n" +
        $"samplesize:         \t{sampleSize}\n" +
        $"channelsInBlock:    \t{ChannelsInBlock}\n\n" +
        $"[Experiment params]\n" +
        $"sample rate:        \t{samplerate}\n" +
        $"Voltage range:      \t{voltageranges[0].VoltageRangeDisplayStringMilliVolt}\n" +
        $"Corresponding to    \t{voltageranges[0].VoltageRangeInMicroVolt} µV\n" +
        $"[Device channel layout]\n\n" +
        $"hardware channels:  \t{hwchannels}\n" +       // 64
        $"analog channels:    \t{ana}\n" +              // 128
        $"digital channels:   \t{digi}\n" +             // 2
        $"che(??) channels:   \t{che}\n" +              // 4
        $"tim(??) channels:   \t{tim}\n\n" +
        $"[Other..]\n" +
        $"valid data bits:    \t{validDataBits}\n" +    // 24
        $"device data format: \t{deviceDataFormat}\n" + // 32
        $"device data mode:   \t{dataMode}\n" +         // dmSigned32bit
        "";

      return true;
    }


    private void _onChannelData(CMcsUsbDacqNet d, int cbHandle, int numSamples){
      try {

        int returnedFrames, totalChannels, offset, channels;

        int handle = 0;
        int channelEntry = 0;
        int frames = 0;


        dataAcquisitionDevice.ChannelBlock_GetChannel
          (handle,
           channelEntry,
           out totalChannels,
           out offset,
           out channels);

        Dictionary<int,int[]> data = dataAcquisitionDevice.ChannelBlock_ReadFramesDictI32
          (handle,
           segmentLength,
           out returnedFrames);


        // Every 40k samples should emit a print.
        // if(this.someCounter > 40000){
        //   this.someCounter = 0;
        //   log.msg("someCounter rolled over 40k");
        //   log.msg($"{returnedFrames}");
        //   log.msg($"{data.Count}");
        // }

        // someCounter += returnedFrames;


        onChannelData(data, returnedFrames);
      }
      catch (Exception e){
        Console.ForegroundColor = ConsoleColor.Red;
        log.err("DAQ ERROR", "DAQ ");
        log.err($"{e}");
        dataAcquisitionDevice.Disconnect();
        throw e;
      }
    }


    private void onError(String msg, int info){
      Console.ForegroundColor = ConsoleColor.Red;
      log.err("DAQ on error invoked :(", "DAQ ");
      log.err($"{info}");
      log.err($"{msg}");

      dataAcquisitionDevice.StopDacq();
      dataAcquisitionDevice.Dispose();
    }
  }
}
