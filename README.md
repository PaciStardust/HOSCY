# HOSCY 

HOSCY is a free and Open-Source tool with many utilities for communication and OSC aimed at making communication and use of OSC easier

If you wish to contribute or need support, join the **[Discord](https://discord.gg/pxwGHvfcxs)**

## Features
- **Speech Recognition**
	- Windows Speech Recognition
	- Locally running AI *(Thanks to [VOSK](https://alphacephei.com/vosk/))*
	- Azure Cognitive Services
	- Most external APIs *(Provided they use raw audio data)*
- **Utility for communicating**
	- A manual textbox for input with preset support
	- A customizable system for displaying Text on VRChats chatbox
	- Integrated Text-to-Speech support
- **Translation** of whatever you say using an external API of your choice
- **OSC** ***(Open Sound Control)*** **Support** using [OSCSharp](https://github.com/ValdemarOrn/SharpOSC)
	- Configurable routing of incomming OSC data
	- Sending out OSC data
	- Creation of your own OSC command sequences
- **Media control** using Voice:
	- Simple and non-intrusive "Now Playing" display 

## Credits
- **[OSCSharp](https://github.com/ValdemarOrn/SharpOSC)** by ValdemarOrn for sending and receiving OSC Data
- **[VOSK](https://alphacephei.com/vosk/)** by AlphaCephei for local AI speech recognition
- **[Azure Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/)** for API speech recognition
- **[AuroraNemoia](https://github.com/AuroraNemoia)** for branding *(Logo, Name)*
- **[Hyblocker](https://github.com/hyblocker)** for providing assistance when I got stuck
- **[Realmlist](https://linktr.ee/Realmlist)** for providing assistance and API keys for testing

## Shortcuts
- **UI Pages**
	- [Main Page](#pages---main)
	- [Input Page](#pages---input)
	- [Speech Page](#pages---speech)
	- [API Page](#pages---api)
	- [Output Page](#pages---output)
	- [OSC Page](#pages---osc)
	- [Debug Page](#pages---debug)
-  **Usage**
	- [Messages and Notifications](#messages-and-notifications)
	- [Commands and Replacements](#commands-and-replacements)
	- [OSC Commands](#osc-commands)
		- [OSC Command Example](#osc-command-example)
		- [OSC Commands Information](#osc-command-information)
	- [OSC Routing](#osc-routing)
		- [Routing received Data](#routing-received-data)
		- [Internal Endpoints](#internal-endpoints)
- **Other**
	- [Speech Recognition](#speech-recognition)
	- [API Preset Configuration](#api-preset-configuration)
		- [Preset Parameters](#preset-parameters)
		- [Preset Examples](#preset-examples)
	- [Text-to-Speech Output](#text-to-speech-output)

# Documentation
## Pages - Main
The main page is a small utility page mostly aimed towards displaying all the information about the current state of the app

It contains information about the last recognized Message and the last sent Notification, as well as an option to quickly mute recognition and clear both Textbox and Text-to-Speech *(To understand the difference between Message and Notification, please see* ***[here](#messages-and-notifications)****)*

## Pages - Input
The input page contains a manual input box for sending out messages over both the Textbox and Text-to-Speech, whichever is used can be chosen with the check boxes "Use TTS" and "Use Textbox"

After sending a message, hitting the send button again will allow you to fill the input box with the last sent message

The right side contains the preset selector, here you can create and use presets for sending messages you might send often *(Something like a greeting for example)*

Additional options like allowing the message to use **[translation](#api-preset-configuration)** or **[commands](#commands-and-replacements)** can be found on the speech page

## Pages - Speech
The speech page contains everything regarding speech recognition and a few options for the input page

Starting the recognizer might freeze the program for a bit depending on the type of recognizer, this is normal. *If you are using an AI model, I highly recommend to monitor your RAM usage during startup*

Here you can also configure your Commands and Replacements as well as the keyword for media control, information about this topic is located **[here](#commands-and-replacements)**

Picking a recognizer will usually gray out some options below as the **[selected recognizer](#speech-recognition)** will not be able to use the options chosen in that section

Recognizer specific options *(like microphone or API info)* only apply after restarting the recognizer, changing them during usage will not apply them

## Pages - API
Here everything API related can be found, if you want to find out how to configure an API Preset, the information is located **[here](#api-preset-configuration)**

Please keep in mind that in order to see the changes made to these options, you usually have to reload whatever is using them. If you are using Translation, simply hit the "Reload" button. The recognition options require you to restart the recognizer if it is running

## Pages - Output
This is the settings page for both the Textbox and Text-to-Speech, I will explain some more hard to explain options here to clarify what they do

- **Dynamic message timeout**
	- Dynamic timeout shows a message / notification not for a set amount of time, but for the time set in "Dynamic Timeout" multiplied for each 20 text characters being displayed
- **Automatic clearing**
	- Enabling these options will automatically clear the textbox when there is nothing more to display, use of this is recommended as it clears up confusion and makes the textbox not as annoying. After a clear the program cannot display any messages for 1 second
- **Show media status**
	- Displays a "now playing" **[notification](#messages-and-notifications)** when changing what you are listening to - This works using windows media so should capture any media, including Youtube
- **Output speakers**
	- This is not intended to be used with normal speakers but instead with a virtual audio cable, for more information please see **[here](#text-to-speech-output)**

## Pages - OSC
Here all the options regarding OSC can be found. Just like with API and recognition, both **[routing](#osc-routing)** and the input port require hitting the little reload button to apply

The output port and IP are used by almost all OSC data that is sent out and are also the default location for **[OSC Commands](#commands-and-replacements)**

## Pages - Debug
This contains all information in terms of logging and debugging, you can also chose for the program to display a console window with the logs, this requires a restart

If you enable the "Debug" log level, I highly recommend to add following filters to avoid massive log spam because of incoming OSC from VRChat:

```
"LogFilter": [
      "/angular",
      "/grounded",
      "/velocity",
      "/upright",
      "/voice",
      "/viseme",
      "/gesture"
]
```
## Messages and Notifications
There is two different systems used in the Textbox to display information, messages and notifications

Messages are used for **[speech recognition](#speech-recognition)** and **[manual input](#pages---input)** and notifications are used by **[media control](#commands)** to show "now playing"

**Messages** work in a queue, as many messages can be stored as needed, which will then be displayed consecutively

There can only be a single **notification** at a time and if the last one has not been displayed yet, but a new notification is put into the system, the old one gets replaced. Notifications only get displayed after all messages have been sent to avoid them from interfering. Notifications are also not affected by **[commands and replacements](#commands-and-replacements)**

Both messages and notifications have separate **[OSC endpoints](#pages---osc)** that can be targeted, it is recommended to use notifications when you want to display short information that is not as important *(like "now playing")*

## Commands and Replacements
Just like there is two different systems for textbox information, there is also two systems for text replacement, commands and replacements

- **Replacements** simply replace parts or an entire message
- **Commands** run some kind of function, not even displaying a message

If these are triggered can be configured in most cases. There usually is also a case-insensitive check box which allows commands and replacements to be detected no matter the capitalization

Replacements are applied before commands are, so in theory it is possible to trigger commands via replacements

### Replacements
Replacements can be found on the **[speech page](#pages---speech)**, they execute in the following order:
- **Noise filters** remove certain words at the start and end of a message *("the I like..." => "I like..")*
- **Replacements** replace a snippet of a message with some other text *("hi" => "hello")*
- **Shortcuts** replace an entire message with some other text *("cat" => "I love cats")*
- **File path** can be provided to read text from a file instead, this can be provided as raw text

### Commands
Commands get run after replacements and are mostly hard-coded, they only trigger when sent as their own message:
- **Skipping messages** can be done by either saying "clear" or "skip"
- **Media Control** can be used to control your currently playing media and can be triggered by saying "media" followed by a command:
	- **Pausing:** "Pause" / "Stop"
	- **Resuming:** "Play" / "Resume"
	- **Skip:** "Next" / "Skip"
	- **Rewind:** "Back" / "Rewind"
-  **OSC** can also executed and sent via commands, how to use this can be learnt **[here](#osc-commands)**

## OSC Commands
OSC Commands are an integrated feature to quickly trigger OSC with a single voice command or simply through the manual input box, to find out how commands work, see **[here](#commands-and-replacements)**

### OSC Command Example
```
[osc] [/hoscy/textbox [s]"Let us dance!" 127.0.0.1:9001] [/avatar/parameters/VRCEmote [i]2 w5000] [/avatar/parameters/VRCEmote [i]0]
```
This is an example of a more complex OSC Command, it does the following:
1. `[osc]` 
	- Indicates that the message is an OSC command
2. `[/hoscy/textbox [s]"Let us dance!" 127.0.0.1:9001]`
	- `/hoscy/textbox` is the target Address
	- `[s]` is an indicator that the following parameter will be a string
	- `"Let us dance!"` is the parameter to be sent
	- `127.0.0.1:9001` is the target IP and port, it needs to be added since we do not want the default output
3. `[/avatar/parameters/VRCEmote [i]2 w5000]`
	- `/avatar/parameters/VRCEmote` is the target Address
	- `[i]` is an indicator that the following parameter will be an integer
	- `2` is the parameter to be sent
	- `w5000` is an indicator to delay the next command by 5000ms
4. `[/avatar/parameters/VRCEmote [i]0]`
	- `/avatar/parameters/VRCEmote` is the target Address
	- `[i]` is an indicator that the following parameter will be an integer
	- `0` is the parameter to be sent

This command sends the text "Lets dance!" through the text box as a message, starts playing the VRChat avatar's second emote, waits 5000ms and then finally resets the emote.

### OSC Command Information
As seen in the example above, the commands follow a fairly simple pattern:
```
[osc] [command] [next command] [another command]
```
The command chain starts with an `[osc]` tag *(case insensitive)*, followed by at least one command, which is always in brackets

The command then follows this pattern:
```
[address [type]value ip:port wait]
```
- **Address** is the target address for the OSC Command
	- This allows any characters that an address is allowed to contain, including wildcards
	- Examples: `/hoscy/textbox` and `/test/*`
- **Type and Value** specify which datatype to send and what its value is *(the type indicator is case insensitive)*
	- **Booleans** have the indicator `[b]` and their value can either be `True`/`true` or `False`/`false`
	- **Integers** have the indicator `[i]` and their value can be any positive or negative whole number
	- **Floats** have the indicator `[f]` and their value can be any positive or negative decimal number
	- **Strings** have the indicator `[s]` and their value can be any string. The value must be surrounded by quotes like this: `"Hello World!"` or it will not be recognized
-  **IP and Address** ***(Optional)*** is the override for the target location
	- If this parameter is not used, the default values from the **[OSC page](#pages---osc)** are used
	- Examples: `127.0.0.1:9001` and `192.168.0.1:9000`
- **Waiting** ***(Optional)*** is a value that can be added at the end of a command to delay the execution of the next in the chain
	- Usage is `w[value]` where `[value]` is replaced by a time in milliseconds
	- Examples: `w100` and `w5000`

## OSC Routing
This can be found on the **[OSC page](#pages---osc)** and handles incoming OSC data, allowing you to route all OSC from one source to multiple destinations.

### Routing Received Data
There is to levels to routing, firstly you set up the actual filter itself.
It only contains a name, which is just used as an identifier, a target IP and port, which is where we want the data to be sent, and finally, it's own filters.

Those filters are kept relatively simple and are just a line of text. If the address of the incoming traffic starts with the text specified in one of the filters, it gets sent over to the destination.

### Internal Endpoints
Additionally there is also some internal endpoints that can be used.
Some of these are specifically targeted towards VRChat input, like buttons for skipping, muting the recognizer or indicating if the recognizer is muted. But you can also access TTS, **[Messages and Notifications](#messages-and-notifications)** with them.

## Speech Recognition
HOSCY has many different ways of recognizing speech (found on the **[speech page](#pages---speech)**, each single one has benefits and drawbacks:

- **Vosk Kaldi AI Recognizer**
	- **Pros:** Local and free
	- **Cons:** Can eat a lot of RAM *(depending on model)*
	- This recognizer runs on most **[Kaldi speech recognition models](https://alphacephei.com/vosk/models)** meaning it can run directly on your machine and is completely free. The larger the model, the higher the quality but also RAM consumption. I recommend this if your PC is capable
- **Windows Speech Recognition**
	- **Pros:** Local and performant
	- **Cons:** Locked to default microphone, slightly inaccurate
	- This recognizer runs uses the default Windows speech recognition system, allowing you to pick any language you have installed. It only works with the default mic and has very mediocre quality
- **Windows Speech Recognition V2**
	- **Pros:** Local and performant
	- **Cons:** Slightly inaccurate
	- This recognizer essentially is the same as above without the microphone restriction. The reason they both exist is because I do not know how reliable this is as I used a rather hacky solution to make it work
- **Any-API Speech Recognition**
	- **Pros:** Fast and performant
	- **Cons:** Not local, likely costs money, not continuous
	- This recognizer can use basically any API to handle the speech recognition. Information about this can be found **[here](#api-preset-configuration)**. This way of recognition only works with audio clips, so the whole clip has to be recorded and will be sent upon mute to be handled.
- **Azure API Recognition**
	- **Pros:** Fast, performant and accurate
	- **Cons:** Not local, costs money
	- This recognizer specifically uses **[Azure cognitive services](https://azure.microsoft.com/en-us/services/cognitive-services/)** for speech recognition, allowing continuous and accurate recognition. The only drawback being the cost. Options can be found **[here](#pages---api)**

## API Preset Configuration
Both translation and any-API recognition require an API preset to function, this topic can be very confusing to anyone not accustomed to working with this so I will try to keep it simple. Preset configuration can be found at the top of the **[API page](#pages---api)**

### Preset Parameters
Ill be explaining the function of every parameter with the **[Azure cognitive services](https://azure.microsoft.com/en-us/services/cognitive-services/)** translation API as an example:

- **Name:** This is the identifier of the preset, you can make this whatever you want as it's only use is logging.
	- Example: `Azure Translation Api (German)`
- **URL:** This is the internet address the data will be sent to for processing, it usually also includes a few parameters.
	- Example: `https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=de`
	- The example above includes the parameters `api-version=3.0` and `to=de`, the latter indicating that it will translate to German
- **Result Name:** The JSON field the API response has the translated text in
	- Example: `text`
- **Timeout:** The timeout in milliseconds before a translation request will stop waiting for a result
	- Example: `2000`
	- I recommend just leaving this at the default value unless you have a bad connection
- **Headers:** These are the headers your request must include, these usually contain some kind of API key to authenticate your connection
	- Examples: `Ocp-Apim-Subscription-Key : <Key>` and `Ocp-Apim-Subscription-Region : northeurope`
	- The example above not only contains a key but a region
- **JSON** ***(Translation only)*****:** This is the JSON data sent to the API
	- Example: `[{ "Text" : "[T]" }]`
	- The example above contains the token `[T]`, this automatically gets replaced with the text that the API will translate
- **Content Type** ***(Speech recognition only)*****:** This is the content type for the raw audio data sent over to the API, most services usually provide what you have to set it as
	- Example: `audio/wav; codecs=audio/pcm; samplerate=16000`
	- The any-API recognizer records audio clips at a sample rate of `16000`, is `mono` channel, and uses `wav` as it's format
	- The example above is used for Azure's recognition as a side note

### Preset Examples
Here are the preset configurations I used for testing.

**[Azure cognitive services - Translation](https://azure.microsoft.com/en-us/services/cognitive-services/)**
```
Name: Azure Translation (DE)
URL: https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=de
Result Name: text
Headers: [Ocp-Apim-Subscription-Key : <Key>], [Ocp-Apim-Subscription-Region : northeurope]
Timeout: 3000
Content Type:
JSON: [{"Text" : "[T]"}]
```

**[Azure cognitive services - Recognition](https://azure.microsoft.com/en-us/services/cognitive-services/)**
```
Name: Azure Recognition
URL: https://northeurope.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US
Result Name: DisplayText
Headers: [Ocp-Apim-Subscription-Key : <Key>], [Accept : true]
Timeout: 3000
Content Type: audio/wav; codecs=audio/pcm; samplerate=16000
JSON:
```

# Extras
A few extra things that are not necessarily part of the documentation but still fairly nice to have anyways can be found here

## Text-to-Speech Output
Due to the limitations of software I can only make the TTS result be output through speakers, so to use this feature you need to install and set up some kind of virtual audio cable, here is the software I can recommend:
 - **[Virtual Audio Cable](https://vb-audio.com/Cable/index.htm)** is just a driver that always runs and simply routes audio from a speaker to a microphone
 - **[Voicemeeter](https://vb-audio.com/Voicemeeter/index.htm)** is an audio mixer that is very popular
 - **[Voicemeeter Banana](https://vb-audio.com/Voicemeeter/banana.htm)** is the same as above but more advanced if needed