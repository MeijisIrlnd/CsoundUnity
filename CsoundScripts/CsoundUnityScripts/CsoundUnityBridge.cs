/*
Copyright (c) <2016> Rory Walsh. 
Android support and asset management changes by Hector Centeno

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR 
ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using csoundcsharp;
#if UNITY_EDITOR || UNITY_STANDALONE
using MYFLT = System.Double;
#elif UNITY_ANDROID
using MYFLT = System.Single;
#endif



/*
 * CsoundUnityBridge class
 */
public class CsoundUnityBridge
{
	public IntPtr csound;
	//ManualResetEvent manualReset;
	public string baseDir;
    //volatile bool shouldFinish=false;
    bool compiledOk = false;

	/* 
		constructor sets up the OPCODE6DIR64 directory that holds the Csound plugins. 
		also creates na instance of Csound and compiles it
	*/
	public CsoundUnityBridge(string csoundDir, string csdFile)
	{
        //manualReset = new ManualResetEvent(false);

		//
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN 
        Csound6.NativeMethods.csoundSetGlobalEnv("OPCODE6DIR64", csoundDir);
        Csound6.NativeMethods.csoundSetGlobalEnv("SFDIR", Application.streamingAssetsPath + "/CsoundFiles");
        Csound6.NativeMethods.csoundSetGlobalEnv("SSDIR", Application.streamingAssetsPath + "/CsoundFiles");
        Csound6.NativeMethods.csoundSetGlobalEnv("SADIR", Application.streamingAssetsPath + "/CsoundFiles");
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		if(Directory.Exists(csoundDir+"/CsoundLib64.framework/Resources/Opcodes64"))
			Csound6.NativeMethods.csoundSetGlobalEnv("OPCODE6DIR64", csoundDir+"/CsoundLib64.framework/Resources/Opcodes64");
#elif UNITY_ANDROID
        Csound6.NativeMethods.csoundSetGlobalEnv("OPCODE6DIR64", csoundDir);
        Csound6.NativeMethods.csoundSetGlobalEnv("SFDIR", Application.persistentDataPath);
        Csound6.NativeMethods.csoundSetGlobalEnv("SSDIR", Application.persistentDataPath);
        Csound6.NativeMethods.csoundSetGlobalEnv("SADIR", Application.persistentDataPath);
#endif
        Csound6.NativeMethods.csoundInitialize(1);
        csound = Csound6.NativeMethods.csoundCreate(System.IntPtr.Zero);
        int systemBufferSize;
        int systemNumBuffers;
        AudioSettings.GetDSPBufferSize(out systemBufferSize, out systemNumBuffers);
        Debug.Log("System buffer size: " + systemBufferSize + ", buffer count: " + systemNumBuffers + " , samplerate: " + AudioSettings.outputSampleRate);
        Csound6.NativeMethods.csoundSetHostImplementedAudioIO(csound, 1, systemBufferSize);
        Csound6.NativeMethods.csoundCreateMessageBuffer(csound, 0);
        string[] runargs = new string[] { "csound", csdFile };
        int ret = Csound6.NativeMethods.csoundCompile(csound, 2, runargs);
        compiledOk = ret == 0 ? true : false;
        Debug.Log("csoundCompile: " + compiledOk);

        //manualReset.Set();  

        //performanceThread = new Thread(new ThreadStart(performCsound));
        //if (ret == 0)
        //    performanceThread.Start();
	}  

	
	public void stopCsound()   
	{
		//manualReset.Reset();
        Csound6.NativeMethods.csoundStop(csound);
        //Csound6.NativeMethods.csoundDestroyMessageBuffer(csound);
        //Csound6.NativeMethods.csoundDestroy(csound);
        
    }
	
	public void reset()
	{
        Csound6.NativeMethods.csoundStop(csound);
        Csound6.NativeMethods.csoundDestroyMessageBuffer(csound);
        Csound6.NativeMethods.csoundDestroy(csound); 
	}
	
    public bool compiledWithoutError()
    {
        return compiledOk;
    }

    public int performKsmps()
    {
       return Csound6.NativeMethods.csoundPerformKsmps(csound);
    }
	
    public MYFLT get0dbfs()
    {
        return Csound6.NativeMethods.csoundGet0dBFS(csound);
    }

    public void setSpinSample(int frame, int channel, MYFLT sample)
    {
        Csound6.NativeMethods.csoundAddSpinSample(csound, frame, channel, sample);
    }

    /// <summary>
    /// Clears the input buffer (spin).
    /// </summary>
    public void ClearSpin()
    {
        Csound6.NativeMethods.csoundClearSpin(csound);
    }

    public void sendScoreEvent(string scoreEvent)
    {
        Csound6.NativeMethods.csoundInputMessage(csound, scoreEvent);
    }

    public void setChannel(string channel, MYFLT value)
	{
		Csound6.NativeMethods.csoundSetControlChannel(csound, channel, value);
	}

	public void setStringChannel(string channel, string value)
	{
		Csound6.NativeMethods.csoundSetStringChannel(csound, channel, value);
	}

	public MYFLT getTable(int table, int index)
	{
		return Csound6.NativeMethods.csoundTableGet(csound, table, index);
	}

    public MYFLT getKr()
    {
        return Csound6.NativeMethods.csoundGetKr(csound);
    }

    public uint getKsmps()
    {
        return Csound6.NativeMethods.csoundGetKsmps(csound);
    }

    public MYFLT getSpoutSample(int frame, int channel)
    {
        return Csound6.NativeMethods.csoundGetSpoutSample(csound, frame, channel);
    }

    public MYFLT getChannel(string channel)
	{
		return Csound6.NativeMethods.csoundGetControlChannel(csound, channel, IntPtr.Zero);
	}

	public int getCsoundMessageCount()
	{
		return Csound6.NativeMethods.csoundGetMessageCnt(csound);
	}
	
	public string getCsoundMessage()
	{
		string message = getMessageText(Csound6.NativeMethods.csoundGetFirstMessage(csound));
		Csound6.NativeMethods.csoundPopFirstMessage(csound);
		return message;
	}

	public static string getMessageText(IntPtr message) {
		int len = 0;
		while (Marshal.ReadByte(message, len) != 0) ++len;
		byte[] buffer = new byte[len];
		Marshal.Copy(message, buffer, 0, buffer.Length);
		return Encoding.UTF8.GetString(buffer);
	}
	
}



