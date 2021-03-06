﻿using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace winforCongitiveTexttoSpeech
{
  public partial class Form1 : Form
  {
    private WaveFileReader waveReader;
    private WaveOut output;

    /// <summary>
    /// This method is called once the audio returned from the service.
    /// It will then attempt to play that audio file.
    /// Note that the playback will fail if the output audio format is not pcm encoded.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The <see cref="GenericEventArgs{Stream}"/> instance containing the event data.</param>
    /// 
    // readStream is the stream you need to read
    // writeStream is the stream you want to write to

    private void PlayAudio(object sender, GenericEventArgs<Stream> args)
    {
      Stream readStream = args.EventData;

      try
      {
        string saveTo = Path.GetDirectoryName(Application.ExecutablePath) + @"\SaveMP3File";  //Folder to Save
        if (!Directory.Exists(saveTo))
        {
          Directory.CreateDirectory(saveTo);
        }
        string filename = saveTo + @"\Shanu" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".mp3"; //Save the speech as mp3 file in root folder

        FileStream writeStream = File.Create(filename);

        int Length = 256;
        Byte[] buffer = new Byte[Length];
        int bytestoRead = readStream.Read(buffer, 0, Length);
        // write the required bytes
        while (bytestoRead > 0)
        {
          writeStream.Write(buffer, 0, bytestoRead);
          bytestoRead = readStream.Read(buffer, 0, Length);
        }

        readStream.Close();
        writeStream.Close();

        // SoundplayerlocationChanged
        // https://docs.microsoft.com/de-de/dotnet/api/system.media.soundplayer?view=netframework-4.7.2

        // Better use NAudio
        // https://stackoverflow.com/questions/22173273/play-sound-in-both-speaker-and-headset-wpf
        // http://mark-dot-net.blogspot.com/2011/05/naudio-audio-output-devices.html


        this.detectDevices(filename);

        //SoundPlayer player = new System.Media.SoundPlayer(filename);
        //player.SoundLocationChanged += new EventHandler(this.player_LocationChanged);
        //player.PlaySync();

      }
      catch (Exception EX)
      {
        txtstatus.Text = EX.Message;
      }
      args.EventData.Dispose();


    }
    private void player_LocationChanged(object sender, EventArgs e)
    {
      //string message = String.Format("SoundLocationChanged: {0}",
      //    player.SoundLocation);
      //ReportStatus(message);
    }

    public void player_LoadCompleted(object sender,
          AsyncCompletedEventArgs e)
    {
      //string message = String.Format("LoadCompleted: {0}",
      //    this.filepathTextbox.Text);
      //ReportStatus(message);
      //EnablePlaybackControls(true);
    }


    /// <summary>
    /// Handler an error when a TTS request failed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="GenericEventArgs{Exception}"/> instance containing the event data.</param>
    private void ErrorHandler(object sender, GenericEventArgs<Exception> e)
    {
      txtstatus.Text = "Unable to complete the TTS request: [{0}]" + e.ToString();
    }

    public Form1()
    {
      InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      cboLocale.SelectedIndex = 0;
      cboServiceName.SelectedIndex = 0;



    }

    private void btnSpeak_Click(object sender, EventArgs e)
    {
      txtstatus.Text = "Starting Authtentication";
      string accessToken;

      // Note: The way to get api key:
      // Free: https://www.microsoft.com/cognitive-services/en-us/subscriptions?productId=/products/Bing.Speech.Preview
      // Paid: https://portal.azure.com/#create/Microsoft.CognitiveServices/apitype/Bing.Speech/pricingtier/S0

      var apiKey = "a5ccddf81d5a465ab84e4241921e487a";

      Authentication auth = new Authentication(apiKey);

      try
      {
        accessToken = auth.GetAccessToken();
        txtstatus.Text = "Token: {0} " + accessToken;
      }
      catch (Exception ex)
      {
        txtstatus.Text = "Failed authentication.";

        txtstatus.Text = ex.Message;
        return;
      }

      txtstatus.Text = "Starting TTSSample request code execution.";

      //https://api.cognitive.microsoft.com/sts/v1.0
      string requestUri = "https://speech.platform.bing.com/synthesize";

      var cortana = new Synthesize();

      cortana.OnAudioAvailable += PlayAudio;
      cortana.OnError += ErrorHandler;

      // Reuse Synthesize object to minimize latency
      cortana.Speak(CancellationToken.None, new Synthesize.InputOptions()
      {
        RequestUri = new Uri(requestUri),
        // Text to be spoken.
        Text = txtSpeak.Text,
        VoiceType = Gender.Female,
        // Refer to the documentation for complete list of supported locales.
        Locale = cboLocale.SelectedItem.ToString(), //"en-US",

        // You can also customize the output voice. Refer to the documentation to view the different
        // voices that the TTS service can output.
        VoiceName = cboServiceName.SelectedItem.ToString(), //"Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)",
                                                            // Service can return audio in different output format.
        OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm,
        AuthorizationToken = "Bearer " + accessToken,
      }).Wait();
    }

    private void cboLocale_SelectedIndexChanged(object sender, EventArgs e)
    {
      cboServiceName.SelectedIndex = cboLocale.SelectedIndex;
    }

    private void cboServiceName_SelectedIndexChanged(object sender, EventArgs e)
    {
      //cboLocale.SelectedIndex = cboServiceName.SelectedIndex;
    }

    private void button1_Click(object sender, EventArgs e)
    {
      //detectDevices("");
    }

    public void detectDevices(string filename)
    {
      int waveOutDevices = WaveOut.DeviceCount;

      for (int i = 0; i < waveOutDevices; i++)
      {
        txtstatus.Text = "Using device " + i;

        var wave1 = new WaveOut();
        wave1.DeviceNumber = 0;
        playSound(0, filename);
        wave1.Dispose();
      }

    }

    public void playSound(int deviceNumber, string fileName)
    {
      disposeWave();// stop previous sounds before starting
      waveReader = new NAudio.Wave.WaveFileReader(fileName);
      var waveOut = new NAudio.Wave.WaveOut();
      waveOut.DeviceNumber = deviceNumber;
      output = waveOut;
      output.Init(waveReader);
      output.Play();
    }


    public void disposeWave()
    {
      if (output != null)
      {
        if (output.PlaybackState == NAudio.Wave.PlaybackState.Playing)
        {
          output.Stop();
          output.Dispose();
          output = null;
        }
      }
      if (waveReader != null)
      {
        waveReader.Dispose();
        waveReader = null;
      }
    }

    private void button2_Click(object sender, EventArgs e)
    {
       //Record().Wait();
      var result =  Record().Result;

      txtstatus.Text = result.Text;

      //var t = Record();
     // t.Start();
    //  t.Wait();
    //  var result = t.Result;
      

    }

    public async Task<SpeechRecognitionResult> Record()
    {
      // Creates an instance of a speech config with specified subscription key and service region.
      // Replace with your own subscription key and service region (e.g., "westus").
      var config = SpeechConfig.FromSubscription("2515f320-76bd-4798-adb3-dade8f1db94e", "northeurope");

      //var config = SpeechConfig.FromEndpoint(new Uri("https://northeurope.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1"), "2515f320-76bd-4798-adb3-dade8f1db94e");

      // Creates a speech recognizer.
      using (var recognizer = new SpeechRecognizer(config))
      {
        Console.WriteLine("Say something...");

        // Performs recognition. RecognizeOnceAsync() returns when the first utterance has been recognized,
        // so it is suitable only for single shot recognition like command or query. For long-running
        // recognition, use StartContinuousRecognitionAsync() instead.
        SpeechRecognitionResult result = await recognizer.RecognizeOnceAsync();

        // Checks result.
        if (result.Reason == ResultReason.RecognizedSpeech)
        {
          Console.WriteLine($"We recognized: {result.Text}");
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
          Console.WriteLine($"NOMATCH: Speech could not be recognized.");
        }
        else if (result.Reason == ResultReason.Canceled)
        {
          var cancellation = CancellationDetails.FromResult(result);
          Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

          if (cancellation.Reason == CancellationReason.Error)
          {
            Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
            Console.WriteLine($"CANCELED: Did you update the subscription info?");
          }
        }

        return result;
      }
    }
  }
}
