### Documentation

-   [Installing](#installing)
-   [Getting started](#getting_started)
-   [Controlling Csound from Unity](#controlling_csound_from_unity)
-   [Controlling Csound from the Unity Editor](#controlling_csound_from_unity_editor)
-   [Troubleshooting](#troubleshooting)
-   [Reference](#reference)

<a name="installing"></a>
## Installing

In order to use CsoundUnity you will need to install the packages available from the following links:

- [CsoundUnity for Windows](https://github.com/rorywalsh/CsoundUnity/releases/download/v2.0/CsoundUnityWin.unitypackage) 

- [CsoundUnity for OSX](https://github.com/rorywalsh/CsoundUnity/releases/download/v2.0/CsoundUnityOSX.unitypackage)

The package comes with a simple scene that demonstrates how it can be used. If you installed CsoundUnity from the Assets Store, you will need to also install Csound. Information on this is given below in the [troubleshooting](#troubleshooting) section.

<a name="getting_started"></a>
## Getting Started

CsoundUnity is a simple component that can be added to any GameObject in a scene. To do so simple hit **AddComponent** in the inspector, then click **Audio** and add **CsoundUnity**.

<img src="http://rorywalsh.github.io/CsoundUnity/images/addCsoundUnityComponent.gif" alt="Add CsoundUnity"/>

CsoundUnity requires the presence of an AudioSource. If the GameObject you are trying to attach a CsoundUnity component to does not already have an AudioSource attached, one will be added automatically. 

Once a CsoundUnity component has been added to a GameObject, you will need to attach a Csound file to it. Csound files *MUST* be placed somewhere in the Assets/Scripts folder. In the sample CsoundUnity package they are kept in a sub-folder within Assets/Scripts called CsoundFiles. This is not required, but may help to better organise your assets. To attach a Csound file to a CsoundUnity component, simply drag it from the Assets folder to the 'Drag and Drop Csound file here' field in the CsoundUnity component inspector. When your game starts, Csound will feed audio from its output buffer into the AudioSource. Any audio produced by Csound can be accessed through the AudioSource component. This is currently restricted to stereo files, but will wor for any amount of channels. See [CsoundUnity::processBlock()](https://github.com/rorywalsh/CsoundUnity/blob/master/CsoundUnityScripts/CsoundUnity.cs#L130-L161) 

<img src="http://rorywalsh.github.io/CsoundUnity/images/addCsoundFile.gif" alt="Add Csound file"/>

Your Csound files should always include the **-n -d** flags in the CsOptions section. 

<img src="http://rorywalsh.github.io/CsoundUnity/images/CsOptions.png" alt="CsOptions"/>

This ensures Csound does not try to open any audio devices. This is left to entirely up to Unity. If you fail to disable writing of audio from Csound to a realtime audio output device, both Csound and Unity will try to access the computer's audio devices at the same time. This will inevitably lead to problems. Any Csound instruments that are set to start after 0 seconds will begin to play as soon as you enter 'Play' mode in Unity. This can be seen in several of the instruments provided in the sample package. You can also start an instrument to play at any time using the [CsoundUnity::sendScoreEvent()](https://github.com/rorywalsh/CsoundUnity/blob/master/CsoundUnityScripts/CsoundUnity.cs#L252-L256) method. 

<a name=controlling_csound_from_unity></a>
## Controlling Csound from Unity 

Once you have attached a Csound file to a CsoundUnity componet, you may wish to control aspects of that instrument in realtime. To do this, you can use Csound's channel system. Csound allows data to be sent and received over its channel system. To access data in Csound, one must use the **chnget** opcode. In the following code example, we access data being sent from Unity to Csoud on a channel named *speed*. The variable kSpeed will constantly update according to the value stored on the channel named *speed*. 

<img src="http://rorywalsh.github.io/CsoundUnity/images/chnget.png" alt="chnget"/>

In order to send data from Unity to Csound we must use the [**CsoundUnity::setChannel(string channel, double value)**](https://github.com/rorywalsh/CsoundUnity/blob/master/CsoundUnityScripts/CsoundUnity.cs#L223-L227) method. Before calling any CsoundUnity methods, one must first access the component using the **GetComponent()** method. This can be seen the simple script that follows. One usually calls GetComponent() in your script's **Awake()** method. Once the CsoundUnity componet has been accessed, any of its member method can be called. In the **update()** method of the script below,     

<img src="http://rorywalsh.github.io/CsoundUnity/images/setChannel.png" alt="setChannel"/>

Channels can also be used to trigger one-shot instruments to play. The simplest way to achieve this is to use Csound's **event** opcode. In the following Csound code example the **JUMP** instrument will be triggered whenever the value of the *"jump"* channel changes. Instrument **TriggerInstrument** will start as soon as the game does. Its job is to listen for changes to the values stored on the channel named *"jump"*. When the channel value changes the variable **kJumpButton** will change, causing the **changed** opcode to output a 1. At this point the code contained within the **if** block will be triggered and the instrument named **JUMP** will be triggered to play for one second. 

<a name=controlling_csound_from_unity_editor></a>
## Controlling Csound from the Unity Editor

In order to control Csound instruments in standalone gameplay, you will need to use the methods described above. However, you can also control Csound channels using the Unity Editor while you are developing your game's sounds. To do so, you must provide a short <CsoundUnity></CsoundUnity> descriptor at the top of your Csound files describing the channels that are needed. This simple descriptor section uses a single line of code to describe each channel. Each line starts with the given channel's controller type and is then followed by combination of other identifiers such as channel(), text(), and range(). The following descriptor sets up 3 channel controllers. A slider, a button and a checkbox(toggle).

<img src="http://rorywalsh.github.io/CsoundUnity/images/csoundUnityDescriptor.png" alt="csoundUnitySection"/>

Each control MUST specify a channel. The range() identifier must be used if the controller type is a slider. The text() identifier can be used to display unique text beside a control but it is not required. If it is left out, the channel() name will be used as the control's label. The caption() identifier, used with form, is used to display some simple help text to the user.

When a Csound file which contains a valid <CsoundUnity> section is dragged to a CsoundUnity component, Unity will generate controls for each channel. These controls can be tweaked when your game is running. Each time a control is modified, its value is sent to Csound from Unity on the associated channel. In this way it works the same as the method above, only we don't have to code anything in order to test our sound. For now, CsoundUnity support only three types of controller, slider, checkbox(toggle) and button. 


<img src="http://rorywalsh.github.io/CsoundUnity/images/csoundUnityDescriptor.gif" alt="csoundUnitySection"/>

<a name=troubleshooting></a>
## Troubleshooting

If you installed CsoundUnity from the Assets Store and don't already have Csound, you should simply install one of the following packages instead:

- [CsoundUnity for Windows](https://github.com/rorywalsh/CsoundUnity/releases/download/v2.0/CsoundUnityWin.unitypackage) 

- [CsoundUnity for OSX](https://github.com/rorywalsh/CsoundUnity/releases/download/v2.0/CsoundUnityOSX.unitypackage)

On Windows, the csound64.dll must reside in the SmtreaingAssets folder. If it is not there, CsoundUnity will display an error. One OSX, the CsoundLib64.framework must reside in the StreamingAssets folder. If it is not, CsoundUnity will display an error. 

If you have Csound installed you can simple place it to the correct location, although it is far easier to use the package provided. They will not have any effect on any existing Csound installation. 

If your game crashes at any point, the best place to look for answers will be in the [Editor Logs](http://docs.unity3d.com/Manual/LogFiles.html). If you continue to have issues with CsoundUnity, please use the github [Issue Tracker](https://github.com/rorywalsh/CsoundUnity/issues) to file an issue. 

<a name=reference></a>
## Reference

Below is a list of all the methods currently available in CsoundUnity. 

```public void setChannel(string channel, float val)```

- Sets a Csound channel. Used in connection with a chnget opcode in your Csound instrument.

```public void setStringChannel(string channel, string val)```

- Sets a string channel in Csound. Used in connection with a chnget opcode in your Csound instrument.

```public void sendScoreEvent(string scoreEvent)```

- Send a score event to Csound in the form of "i1 0 10 ...."

```public double getChannel(string channel)```

- Gets a Csound channel. Used in connection with a chnset opcode in your Csound instrument.

```public double getTable(int tableNumber, int index)```

- Retrieves a single sample from a Csound function table. 

```public double get0dbfs()```

- Get 0dbfs

```public int performKsmps()```

- Processes a ksmps-sized block of samples

```public double setKr()```

- Return the current control rate

```public uint getKsmps()```

- Get the current control rate

```public void processBlock(float[] samples, int numChannels)```

- Processes a block of samples

```public double getSample(int frame, int channel)```

- Get a sample from Csound's audio output buffer

```public void setSample(int frame, int channel, double sample)```

- Set a sample in Csound's input buffer	