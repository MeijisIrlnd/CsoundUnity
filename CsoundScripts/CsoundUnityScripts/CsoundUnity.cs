/*
Copyright (C) 2015 Rory Walsh. 
Android support and asset management changes by Hector Centeno

This interface would not have been possible without Richard Henninger's .NET interface to the Csound API. 
 
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
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System;
#endif
#if UNITY_ANDROID
using System;
using UnityEngine.Networking;
#endif
#if UNITY_EDITOR || UNITY_STANDALONE
using MYFLT = System.Double;
#elif UNITY_ANDROID // and maybe iOS?
using MYFLT = System.Single;
#endif

[Serializable]
[SerializeField]
/// <summary>
/// Utility class for controller and channels
/// </summary>
public class CsoundChannelController
{
    [SerializeField] public string type = "";
    [SerializeField] public string channel = "";
    [SerializeField] public string text = "";
    [SerializeField] public string caption = "";
    [SerializeField] public float min;
    [SerializeField] public float max;
    [SerializeField] public float value;
    [SerializeField] public float skew;
    [SerializeField] public float increment;
    [SerializeField] public string[] options;

    public void SetRange(float uMin, float uMax, float uValue)
    {
        min = uMin;
        max = uMax;
        value = uValue;
    }
}

[Serializable]
public struct CsoundFilesInfo
{
    public string[] fileNames;
}


/*
 * CsoundUnity class
 */
[AddComponentMenu("Audio/CsoundUnity")]
[System.Serializable]
[RequireComponent(typeof(AudioSource))]
public class CsoundUnity : MonoBehaviour
{
    #region PUBLIC_FIELDS

    /// <summary>
    /// The name of this package
    /// </summary>
    public const string packageName = "com.csound.csoundunity";

    /// <summary>
    /// The version of this package
    /// </summary>
    public const string packageVersion = "1.0.0";

    /// <summary>
    /// the unique guid of the csd file
    /// </summary>
    public string csoundFileGUID { get => _csoundFileGUID; }

    /// <summary>
    /// The file CsoundUnity will try to load. You can only load one file with each instance of CsoundUnity,
    /// but you can use as many instruments within that file as you wish.You may also create as many
    /// of CsoundUnity objects as you wish. 
    /// </summary>
    public string csoundFileName { get => _csoundFileName; }

    /// <summary>
    /// a string to hold all the csoundFile content
    /// </summary>
    public string csoundString { get => _csoundString; }

#if UNITY_EDITOR
    /// <summary>
    /// a reference to a csd file as DefaultAsset 
    /// </summary>
    //[SerializeField]
    public DefaultAsset csoundAsset { get => _csoundAsset; }
#endif

    /// <summary>
    /// When it is set to true, all Csound output messages will be sent to the 
    /// Unity output console.
    /// Note that this can slow down performance if there is a
    /// lot of information being printed.
    /// </summary>
    public bool logCsoundOutput = false;

    /// <summary>
    /// When it is set to true, no audio is sent to output
    /// </summary>
    public bool mute = false;

    /// <summary>
    /// When set to true Csound uses as an input the AudioClip attached to this AudioSource
    /// If false, no processing occurs on the attached AudioClip
    /// </summary>
    public bool processClipAudio;

    /// <summary>
    /// list to hold channel data
    /// </summary>
    public List<CsoundChannelController> channels { get => _channels; }

    /// <summary>
    /// list to hold available audioChannels names
    /// </summary>
    public List<string> availableAudioChannels { get => _availableAudioChannels; }

    /// <summary>
    /// public named audio Channels shown in CsoundUnityChild inspector
    /// </summary>
    public readonly Dictionary<string, MYFLT[]> namedAudioChannelDataDict = new Dictionary<string, MYFLT[]>();

    /// <summary>
    /// Is Csound initialized?
    /// </summary>
    public bool IsInitialized { get => initialized; }

    /// <summary>
    /// The delegate of the event OnCsoundInitialized
    /// </summary>
    public delegate void CsoundInitialized();
    /// <summary>
    /// An event that will be executed when Csound is initialized
    /// </summary>
    public event CsoundInitialized OnCsoundInitialized;

    public bool PerformanceFinished { get => performanceFinished; }
    /// <summary>
    /// the score to send via editor
    /// </summary>
    public string csoundScore;

    #endregion PUBLIC_FIELDS

    #region PRIVATE_FIELDS

    /// <summary>
    /// The private member variable csound provides access to the CsoundUnityBridge class, which 
    /// is defined in the CsoundUnity native libraries. If for some reason the libraries can 
    /// not be found, Unity will report the issue in its output Console. The CsoundUnityBridge object
    /// provides access to Csounds low level native functions. The csound object is defined as private,
    /// meaning other scripts cannot access it. If other scripts need to call any of Csounds native
    /// fuctions, then methods should be added to the CsoundUnity.cs file and CsoundUnityBridge class.
    /// </summary>
    private CsoundUnityBridge csound;
    [SerializeField] private string _csoundFileGUID;
    [SerializeField] private string _csoundString;
    [SerializeField] private string _csoundFileName;
#if UNITY_EDITOR
    [SerializeField] private DefaultAsset _csoundAsset;
#endif
    [SerializeField] private List<CsoundChannelController> _channels = new List<CsoundChannelController>();
    [SerializeField] private List<string> _availableAudioChannels = new List<string>();
    /// <summary>
    /// Inspector foldout settings
    /// </summary>
#pragma warning disable 414
    [SerializeField] private bool _drawCsoundString = false;
    [SerializeField] private bool _drawTestScore = false;
    [SerializeField] private bool _drawSettings = false;
    [SerializeField] private bool _drawChannels = false;
    [SerializeField] private bool _drawAudioChannels = false;
#pragma warning restore 414

    private bool initialized = false;
    private uint ksmps = 32;
    private uint ksmpsIndex = 0;
    private float zerdbfs = 1;
    private bool compiledOk = false;
    private bool performanceFinished;
    private AudioSource audioSource;
    private Coroutine LoggingCoroutine;
    int bufferSize, numBuffers;

    /// <summary>
    /// the temp buffer, ksmps sized 
    /// </summary>
    private Dictionary<string, MYFLT[]> namedAudioChannelTempBufferDict = new Dictionary<string, MYFLT[]>();

    #endregion

    private AudioClip dummyClip;

    /**
     * CsoundUnity Awake function. Called when this script is first instantiated. This should never be called directly. 
     * This functions behaves in more or less the same way as a class constructor. When creating references to the
     * CsoundUnity object make sure to create them in the scripts Awake() function.
     * 
     */
    void Awake()
    {
        Debug.Log("AudioSettings.outputSampleRate: " + AudioSettings.outputSampleRate);

        AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);

        string dataPath = Path.GetFullPath(Path.Combine("Packages", packageName, "Runtime"));


#if UNITY_EDITOR || UNITY_STANDALONE
        string csoundFilePath = Application.streamingAssetsPath + "/CsoundFiles/" + csoundFileName;
#if UNITY_EDITOR
        dataPath = Application.dataPath + "/Plugins/Win64/CsoundPlugins"; // Csound plugin libraries in Editor
#elif UNITY_STANDALONE_WIN
        dataPath = Application.dataPath + "/Plugins"; // Csound plugin libraries get copied to the root of plugins directory in the application data directory 
#endif
        string path = System.Environment.GetEnvironmentVariable("Path");
        if (string.IsNullOrWhiteSpace(path) || !path.Contains(dataPath))
        {
            string updatedPath = path + ";" + dataPath;
            print("Updated path:" + updatedPath);
            System.Environment.SetEnvironmentVariable("Path", updatedPath); // Is this needed for Csound to find libraries?
        }
#elif UNITY_ANDROID
        // Copy CSD to persistent data storage
        string csoundFileTmp = "jar:file://" + Application.dataPath + "!/assets/CsoundFiles/" + csoundFileName;
        UnityWebRequest webrequest = UnityWebRequest.Get(csoundFileTmp);
        webrequest.SendWebRequest();
        while (!webrequest.isDone) { }
        string csoundFilePath = getCsoundFile(webrequest.downloadHandler.text);
        dataPath = "not needed for Android";

        // Copy all audio files to persistent data storage
        csoundFileTmp = "jar:file://" + Application.dataPath + "!/assets/CsoundFiles/csoundFiles.json";
        webrequest = UnityWebRequest.Get(csoundFileTmp);
        webrequest.SendWebRequest();
        while (!webrequest.isDone) { }
        CsoundFilesInfo filesObj = JsonUtility.FromJson<CsoundFilesInfo>(webrequest.downloadHandler.text);
        foreach (var item in filesObj.fileNames)
        {
            if (!item.EndsWith(".json") && !item.EndsWith(".meta") && !item.EndsWith(".csd") && !item.EndsWith(".orc"))
            {
                csoundFileTmp = "jar:file://" + Application.dataPath + "!/assets/CsoundFiles/" + item;
                webrequest = UnityWebRequest.Get(csoundFileTmp);
                webrequest.SendWebRequest();
                while (!webrequest.isDone) { }
                getCsoundAudioFile(webrequest.downloadHandler.data, item);
            }
        }
#endif

        audioSource = GetComponent<AudioSource>();

        /*
         * the CsoundUnity constructor takes a path to the project's Data folder, and path to the file name.
         * It then calls createCsound() to create an instance of Csound and compile the 'csdFile'. 
         * After this we start the performance of Csound. After this, we send the streaming assets path to
         * Csound on a string channel. This means we can then load samples contained within that folder.
         */
        csound = new CsoundUnityBridge(dataPath, csoundFilePath);
        if (csound != null)
        {
            if(channels != null)
            {
                for(var i = 0; i < channels.Count; i++)
                {
                    csound.setChannel(channels[i].channel, channels[i].value);
                }
            }
            //channels = parseCsdFile(csoundFilePath);
            //initialise channels if found in xml descriptor..
            foreach(var name in availableAudioChannels)
            {
                if(!namedAudioChannelDataDict.ContainsKey(name))
                {
                    namedAudioChannelDataDict.Add(name, new MYFLT[bufferSize]);
                    namedAudioChannelTempBufferDict.Add(name, new MYFLT[ksmps]);
                }
            }

            /*
             * This method prints the Csound output to the Unity console
             */
            if (logCsoundOutput)
                InvokeRepeating("logCsoundMessages", 0, .5f);

            compiledOk = csound.compiledWithoutError();

            if (compiledOk)
            {
                zerdbfs = (float)csound.get0dbfs();
                Debug.Log("zerdbfs " + zerdbfs);
#if UNITY_EDITOR || UNITY_STANDALONE
                csound.setStringChannel("CsoundFiles", Application.streamingAssetsPath + "/CsoundFiles/");
                csound.setStringChannel("AudioPath", Application.dataPath + "/Audio/");
                if (Application.isEditor)
                    csound.setStringChannel("CsoundFiles", Application.streamingAssetsPath + "/CsoundFiles/");
                else
                    csound.setStringChannel("CsoundFiles", Application.streamingAssetsPath + "/CsoundFiles/");
                csound.setStringChannel("StreamingAssets", Application.streamingAssetsPath);
                //csound.setStringChannel("AudioPath", Application.dataPath + "/Audio/");
                //if (Application.isEditor)
                //    csound.setStringChannel("CsoundFiles", Application.streamingAssetsPath + "/CsoundFiles/");
                //else
                //    csound.setStringChannel("CsoundFiles", Application.streamingAssetsPath + "/CsoundFiles/");
                //csound.setStringChannel("StreamingAssets", Application.streamingAssetsPath);
#elif UNITY_ANDROID
                string persistentPath = Application.persistentDataPath + "/CsoundFiles/";
                csound.setStringChannel("CsoundFilesPath", Application.dataPath);
#endif
                initialized = true;
                OnCsoundInitialized?.Invoke();
            }
        }
        else
        {
            Debug.Log("Error creating Csound object");
            compiledOk = false;
        }

        Debug.Log("CsoundUnity done init");
    }

#if UNITY_ANDROID

    /**
     * Android method to write csd file to a location it can be read from Method returns the file path. 
     */
    public string getCsoundFile(string csoundFileContents)
    {
        try
        {
            Debug.Log("Csound file contents:");
            Debug.Log(csoundFileContents);
            string filename = Application.persistentDataPath + "/csoundFile.csd";
            Debug.Log("Writing to " + filename);

            if (!File.Exists(filename))
            {
                Debug.Log("File doesnt exist, creating it");
                File.Create(filename).Close();
            }

            if (File.Exists(filename))
            {
                Debug.Log("File has been created");
            }

            File.WriteAllText(filename, csoundFileContents);
            return filename;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error writing to file: " + e.ToString());
        }

        return "";
    }

    public void getCsoundAudioFile(byte[] data, string filename)
    {
        try
        {
            string name = Application.persistentDataPath + "/" + filename;
            File.Create(name).Close();
            File.WriteAllBytes(name, data);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error writing to file: " + e.ToString());
        }
    }
#endif

    /**
     * Called automatically when the game stops. Needed so that Csound stops when your game does
     */
    void OnApplicationQuit()
    {
        if (csound != null)
        {
            csound.stopCsound();
        }

        //csound.reset();
    }


    /**
     * Get the current control rate
     */
    public MYFLT setKr()
    {
        return csound.getKr();
    }


    /**
     * this gets called for every block of samples
     */
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (csound != null)
        {
            processBlock(data, channels);
        }

    }

    /**
    * Processes a block of samples
    */
    private void processBlock(float[] samples, int numChannels)
    {
        if (compiledOk)
        {
            for (int i = 0; i < samples.Length; i += numChannels, ksmpsIndex += (uint)numChannels)
            {
                for (int channel = 0; channel < numChannels; channel++)
                {
                    if (mute == true)
                        samples[i + channel] = 0.0f;
                    else
                    {
                        if (processClipAudio)
                            setSample(i + channel, channel, samples[i + channel]);

                        if ((ksmpsIndex >= ksmps) && (ksmps > 0))
                        {
                            performKsmps();
                            ksmpsIndex = 0;
                        }

                        samples[i + channel] = (float)(getSample((int)ksmpsIndex, channel) / zerdbfs);
                    }
                }
            }
        }
    }

    /**
     * Get a sample from Csound's audio output buffer
     */
    public MYFLT getSample(int frame, int channel)
    {
        return csound.getSpoutSample(frame, channel);
    }

    /**
     * Set a sample in Csound's input buffer
     */
    public void setSample(int frame, int channel, MYFLT sample)
    {
        csound.setSpinSample(frame, channel, sample);
    }

    /**
     * process a ksmps-sized block of samples
     */
    public int performKsmps()
    {
        return csound.performKsmps();
    }

    /**
     * Get the current control rate
     */
    public uint getKsmps()
    {
        return csound.getKsmps();
    }

    /**
        * Get a sample from Csound's audio output buffer
    */
    public MYFLT getOutputSample(int frame, int channel)
    {
        return csound.getSpoutSample(frame, channel);
    }

    /**
     * Get 0 dbfs
     */
    public MYFLT get0dbfs()
    {
        return csound.get0dbfs();
    }

    /**
    * get file path
    */
#if UNITY_EDITOR
    public string getFilePath(UnityEngine.Object obj)
    {
        return Application.dataPath.Replace("Assets", "") + AssetDatabase.GetAssetPath(obj);
    }
#endif
    /**
     * map MYFLT within one range to another 
     */

    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        float retValue = (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        return Mathf.Clamp(retValue, from2, to2);
    }

    /**
     * Sets a Csound channel. Used in connection with a chnget opcode in your Csound instrument.
     */
    public void setChannel(string channel, MYFLT val)
    {
        csound.setChannel(channel, val);
    }
    /**
     * Sets a string channel in Csound. Used in connection with a chnget opcode in your Csound instrument.
     */
    public void setStringChannel(string channel, string val)
    {
        csound.setStringChannel(channel, val);
    }
    /**
     * Gets a Csound channel. Used in connection with a chnset opcode in your Csound instrument.
     */
    public MYFLT getChannel(string channel)
    {
        return csound.getChannel(channel);
    }

    /**
     * Retrieves a single sample from a Csound function table. 
     */
    public MYFLT getTableSample(int tableNumber, int index)
    {
        return csound.getTable(tableNumber, index);
    }
    /**
     * Send a score event to Csound in the form of "i1 0 10 ...."
     */
    public void sendScoreEvent(string scoreEvent)
    {
        //print(scoreEvent);
        csound.sendScoreEvent(scoreEvent);
    }

    /**
     * Print the Csound output to the Unity message console. No need to call this manually, it is set up and controlled in the CsoundUnity Awake() function.
     */
    void logCsoundMessages()
    {
        //print Csound message to Unity console....
        for (int i = 0; i < csound.getCsoundMessageCount(); i++)
            print(csound.getCsoundMessage());
    }


    public List<CsoundChannelController> parseCsdFile(string filename)
    {
        string[] fullCsdText = File.ReadAllLines(filename);
        List<CsoundChannelController> locaChannelControllers;
        locaChannelControllers = new List<CsoundChannelController>();

        foreach (string line in fullCsdText)
        {
            if (line.Contains("</"))
                break;

            string newLine = line;
            string control = line.Substring(0, line.IndexOf(" ") > -1 ? line.IndexOf(" ") : 0);
            if (control.Length > 0)
                newLine = newLine.Replace(control, "");


            if (control.Contains("slider") || control.Contains("button") || control.Contains("checkbox") || control.Contains("groupbox") || control.Contains("form"))
            {
                CsoundChannelController controller = new CsoundChannelController();
                controller.type = control;

                if (line.IndexOf("caption(") > -1)
                {
                    string infoText = line.Substring(line.IndexOf("caption(") + 9);
                    infoText = infoText.Substring(0, infoText.IndexOf(")") - 1);
                    controller.caption = infoText;
                }

                if (line.IndexOf("text(") > -1)
                {
                    string text = line.Substring(line.IndexOf("text(") + 6);
                    text = text.Substring(0, text.IndexOf(")") - 1);
                    controller.text = text;
                }

                if (line.IndexOf("channel(") > -1)
                {
                    string channel = line.Substring(line.IndexOf("channel(") + 9);
                    channel = channel.Substring(0, channel.IndexOf(")") - 1);
                    controller.channel = channel;
                }

                if (line.IndexOf("range(") > -1)
                {
                    string range = line.Substring(line.IndexOf("range(") + 6);
                    range = range.Substring(0, range.IndexOf(")"));
                    char[] delimiterChars = { ',' };
                    string[] tokens = range.Split(delimiterChars);
                    controller.SetRange(float.Parse(tokens[0]), float.Parse(tokens[1]), float.Parse(tokens[2]));
                }

                if (line.IndexOf("value(") > -1)
                {
                    string value = line.Substring(line.IndexOf("value(") + 6);
                    value = value.Substring(0, value.IndexOf(")"));
                    controller.value = value.Length > 0 ? float.Parse(value) : 0;
                }

                locaChannelControllers.Add(controller);
            }
        }
        return locaChannelControllers;
    }

    /// <summary>
    /// Sets the csd file 
    /// </summary>
    /// <param name="guid">the guid of the csd file asset</param>
    public void SetCsd(string guid)
    {
#if UNITY_EDITOR //for now setting csd is permitted from editor only, via asset guid

        // Debug.Log($"SET CSD guid: {guid}");
        if (string.IsNullOrWhiteSpace(guid) || !Guid.TryParse(guid, out Guid guidResult))
        {
            Debug.LogWarning($"GUID NOT VALID Resetting fields");
            ResetFields();
            return;
        }

        var fileName = AssetDatabase.GUIDToAssetPath(guid);

        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Length < 4 ||
            Path.GetFileName(fileName).Length < 4 ||
            !Path.GetFileName(fileName).EndsWith(".csd"))
        {
            Debug.LogWarning("FILENAME not valid, Resetting fields");
            ResetFields();
            return;
        }

        this._csoundFileGUID = guid;
        this._csoundFileName = Path.GetFileName(fileName);
        var csoundFilePath = Path.GetFullPath(fileName);
        this._csoundAsset = (DefaultAsset)(AssetDatabase.LoadAssetAtPath(fileName, typeof(DefaultAsset)));
        this._csoundString = File.ReadAllText(csoundFilePath);
        this._channels = parseCsdFile(fileName);
        this._availableAudioChannels = ParseCsdFileForAudioChannels(fileName);

        foreach (var name in availableAudioChannels)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (!namedAudioChannelDataDict.ContainsKey(name))
            {
                namedAudioChannelDataDict.Add(name, new MYFLT[bufferSize]);
                namedAudioChannelTempBufferDict.Add(name, new MYFLT[ksmps]);
            }
        }

#endif
    }

    /// <summary>
    /// Reset the fields of this instance
    /// </summary>
    private void ResetFields()
    {
#if UNITY_EDITOR
        this._csoundAsset = null;
#endif

        this._csoundFileName = null;
        this._csoundString = null;
        this._csoundFileGUID = string.Empty;

        this._channels.Clear();
        this._availableAudioChannels.Clear();

        this.namedAudioChannelDataDict.Clear();
        this.namedAudioChannelTempBufferDict.Clear();
    }

    /// <summary>
    /// Parse the csd and returns available audio channels (set in csd via: <code>chnset avar, "audio channel name") </code>
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static List<string> ParseCsdFileForAudioChannels(string filename)
    {
        if (!File.Exists(filename)) return null;

        string[] fullCsdText = File.ReadAllLines(filename);
        if (fullCsdText.Length < 1) return null;

        List<string> locaAudioChannels = new List<string>();

        foreach (string line in fullCsdText)
        {
            var trimmd = line.TrimStart();
            if (!trimmd.Contains("chnset")) continue;
            if (trimmd.StartsWith(";")) continue;
            var lndx = trimmd.IndexOf("chnset");
            var chnsetEnd = lndx + "chnset".Length + 1;
            var prms = trimmd.Substring(chnsetEnd, trimmd.Length - chnsetEnd);
            var split = prms.Split(',');
            if (!split[0].StartsWith("a") && !split[0].StartsWith("ga"))
                continue; //discard non audio variables
            // Debug.Log("found audio channel");
            var ach = split[1].Replace('\\', ' ').Replace('\"', ' ').Trim();
            if (!locaAudioChannels.Contains(ach))
                locaAudioChannels.Add(ach);
        }
        return locaAudioChannels;
    }

    /// <summary>
    /// Clears the input buffer (spin).
    /// </summary>
    public void ClearSpin()
    {
        if (csound != null)
        {
            Debug.Log("clear spin");
            csound.ClearSpin();
        }
    }
}
