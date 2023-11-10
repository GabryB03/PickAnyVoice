using MetroSuite;
using Microsoft.VisualBasic;
using NAudio.Wave;
using PickAnyVoice.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public partial class MainForm : MetroForm
{
    private List<string> _theVoices;
    private char[] _filteringCharacters = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private string _currentVoiceName;
    private WaveIn _waveIn;
    private WaveFileWriter _waveFileWriter;
    private Process _process, _inferWaiter;
    private WaveOutEvent _waveOutEvent;
    private AudioFileReader _audioFileReader;
    private List<EdgeTtsVoice> _edgeTtsVoices;
    private List<GoogleTtsVoice> _googleTtsVoices;

    private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public MainForm()
    {
        InitializeComponent();
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
        CheckForIllegalCrossThreadCalls = false;
        _theVoices = new List<string>();

        foreach (string directory in Directory.GetDirectories("data\\voices"))
        {
            string voiceName = Path.GetFileName(directory);
            listBox1.Items.Add(voiceName);
            _theVoices.Add(voiceName);
        }

        for (int waveInDevice = 0; waveInDevice < WaveIn.DeviceCount; waveInDevice++)
        {
            guna2ComboBox1.Items.Add(WaveIn.GetCapabilities(waveInDevice).ProductName);
        }

        for (int waveOutDevice = 0; waveOutDevice < WaveOut.DeviceCount; waveOutDevice++)
        {
            guna2ComboBox2.Items.Add(WaveOut.GetCapabilities(waveOutDevice).ProductName);
        }

        File.WriteAllBytes("data\\runtime\\_infer_waiter_.py", PickAnyVoice.Properties.Resources.pythoninfer);
        File.WriteAllBytes("data\\runtime\\gui_v2.py", PickAnyVoice.Properties.Resources.V2PYTHON);
        File.WriteAllText("data\\runtime\\gui_v2.bat", PickAnyVoice.Properties.Resources.V2BAT);
        File.WriteAllText("data\\runtime\\_infer_waiter_.bat", "runtime\\python.exe _infer_waiter_.py --pycmd runtime\\python.exe\r\npause");

        CloseAllPythonInstances();
        DeleteTempFiles(true);
        StartInferWaiter();

        Thread thread1 = new Thread(CheckForNewVoices);
        thread1.Priority = ThreadPriority.Highest;
        thread1.Start();

        _edgeTtsVoices = new List<EdgeTtsVoice>();
        _googleTtsVoices = new List<GoogleTtsVoice>();

        string[] edgeTtsLines = File.ReadAllLines("data\\tts\\edgetts.txt");

        foreach (string line in edgeTtsLines)
        {
            string[] splitted1 = line.Split(' ');
            string voiceName = splitted1[0];

            string[] splitted2 = Strings.Split(line, " | ");
            string completeVoiceName = splitted2[1];

            string[] splitted3 = line.Split('(');
            string[] splitted4 = splitted3[1].Split(')');
            string[] splitted5 = Strings.Split(splitted4[0], ", ");

            string language = splitted5[0], gender = splitted5[1];
            _edgeTtsVoices.Add(new EdgeTtsVoice(voiceName, completeVoiceName, gender, language));
        }

        string[] googleTtsLines = File.ReadAllLines("data\\tts\\googletts.txt");

        foreach (string line in googleTtsLines)
        {
            string[] splitted = Strings.Split(line, ": ");
            string languageCode = splitted[0], languageName = splitted[1];
            _googleTtsVoices.Add(new GoogleTtsVoice(languageCode, languageName));
        }

        guna2ComboBox7.Items.Add("None");

        foreach (string dir in Directory.GetFiles("data\\custom_index"))
        {
            guna2ComboBox7.Items.Add(Path.GetFileNameWithoutExtension(dir));
        }

        guna2ComboBox1.SelectedIndex = 0;
        guna2ComboBox2.SelectedIndex = 0;
        guna2ComboBox3.SelectedIndex = 12;
        guna2ComboBox4.SelectedIndex = 0;
        guna2ComboBox7.SelectedIndex = 0;
    }

    private void StartInferWaiter()
    {
        _inferWaiter = new Process();
        _inferWaiter.StartInfo.FileName = "_infer_waiter_.bat";
        _inferWaiter.StartInfo.WorkingDirectory = Path.GetFullPath("data\\runtime");
        _inferWaiter.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        _inferWaiter.StartInfo.CreateNoWindow = true;
        _inferWaiter.Start();
    }

    private void CheckForNewVoices()
    {
        while (true)
        {
            Thread.Sleep(1000);
            string[] dirs = Directory.GetDirectories("data\\voices");

            if (dirs.Length != _theVoices.Count)
            {
                _theVoices.Clear();
                listBox1.Items.Clear();

                foreach (string dir in dirs)
                {
                    listBox1.Items.Add(Path.GetFileName(dir));
                    _theVoices.Add(Path.GetFileName(dir));
                }
            }

            foreach (string dir in dirs)
            {
                foreach (string theFile in Directory.GetFiles(dir))
                {
                    string extension = Path.GetExtension(theFile).ToLower();

                    if (extension.Equals(".pth") && !Path.GetFileNameWithoutExtension(theFile).Equals("rvcmodelvoice"))
                    {
                        File.Copy(theFile, dir + "\\rvcmodelvoice.pth");
                        File.Delete(theFile);
                    }

                    if (extension.Equals(".index") && !Path.GetFileNameWithoutExtension(theFile).Equals("rvcmodelvoice"))
                    {
                        File.Copy(theFile, dir + "\\rvcmodelvoice.index");
                        File.Delete(theFile);
                    }

                    if (Path.GetFileNameWithoutExtension(theFile).Equals("avatar") && !extension.Equals(".jpg"))
                    {
                        RunFFMpeg($"-i \"{Path.GetFullPath(theFile)}\" \"{Path.GetFullPath(theFile).ToLower().Replace(extension, "") + ".jpg"}\"");
                        File.Delete(theFile);
                    }
                }

                foreach (string theFile in Directory.GetFiles(dir))
                {
                    string lowered = Path.GetFileName(theFile).ToLower();

                    if (!lowered.Equals("rvcmodelvoice.pth") && !lowered.Equals("rvcmodelvoice.index") && !lowered.Equals("exampleaudio.wav") && !lowered.Equals("avatar.jpg"))
                    {
                        File.Delete(theFile);
                    }
                }

                if (!File.Exists(dir + "\\exampleaudio.wav") && File.Exists(dir + "\\rvcmodelvoice.pth") && File.Exists(dir + "\\rvcmodelvoice.index"))
                {
                    DeleteTempFiles();
                    MessageBox.Show($"Press OK to generate example audio of the new added voice called \"{Path.GetFileName(dir)}\".", "PickAnyVoice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); ;
                    
                    File.Copy(dir + "\\rvcmodelvoice.pth", "C:\\rvcmodelvoice.pth");
                    File.Copy(dir + "\\rvcmodelvoice.index", "C:\\rvcmodelvoice.index");

                    CompressAudioFile(Path.GetFullPath("data\\exampleaudio.wav"), "C:\\input.wav");
                    RunInference("C:\\input.wav", "C:\\output.wav");
                    CompressAudioFile("C:\\output.wav", Path.GetFullPath(dir + "\\exampleaudio.wav"));
                    DeleteTempFiles();
                    MessageBox.Show("Succesfully generated the example audio.", "PickAnyVoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }

    private void guna2TextBox1_TextChanged(object sender, System.EventArgs e)
    {
        listBox1.Items.Clear();

        foreach (string voiceName in _theVoices)
        {
            if (FilterString(voiceName).Contains(FilterString(guna2TextBox1.Text)) || FilterString(guna2TextBox1.Text).Contains(FilterString(voiceName)))
            {
                listBox1.Items.Add(voiceName);
            }
        }
    }

    private string FilterString(string str)
    {
        string result = "";
        str = str.ToLower();

        foreach (char c in str)
        {
            foreach (char s in _filteringCharacters)
            {
                if (c.Equals(s))
                {
                    result += c;
                    break;
                }
            }
        }

        return result;
    }

    private void guna2Button2_Click(object sender, System.EventArgs e)
    {
        if (guna2Button2.Text == "Hear a sample of the picked voice")
        {
            guna2Button2.Text = "Stop hearing the current sample";
            PlayWavAudioFile(Path.GetFullPath($"data\\voices\\{_currentVoiceName}\\exampleaudio.wav"));
        }
        else
        {
            StopPlayingAudio();
            guna2Button2.Text = "Hear a sample of the picked voice";
        }
    }

    private void guna2Button3_Click(object sender, System.EventArgs e)
    {
        Process.Start($"data\\voices\\{_currentVoiceName}");
    }

    private void listBox1_SelectedIndexChanged(object sender, System.EventArgs e)
    {
        if (listBox1.SelectedItem != null && !listBox1.SelectedItem.ToString().Equals(_currentVoiceName))
        {
            if (listBox1.SelectedItem == null)
            {
                return;
            }

            StopPlayingAudio();
            guna2Button2.Text = "Hear a sample of the picked voice";
            _currentVoiceName = listBox1.SelectedItem.ToString();
            guna2Button1.Enabled = true;
            guna2Button2.Enabled = true;
            guna2Button3.Enabled = true;
            guna2Button4.Enabled = true;
            guna2Button6.Enabled = true;
            guna2Button7.Enabled = true;
            label4.Text = _currentVoiceName;

            if (File.Exists($"data\\voices\\{_currentVoiceName}\\avatar.jpg"))
            {
                guna2CirclePictureBox1.Image = Image.FromFile($"data\\voices\\{_currentVoiceName}\\avatar.jpg");
            }
            else
            {
                guna2CirclePictureBox1.Image = null;
            }

            DeleteTempFiles(true);
            File.Copy($"data\\voices\\{_currentVoiceName}\\rvcmodelvoice.pth", "C:\\rvcmodelvoice.pth");
            
            if (guna2ComboBox7.SelectedIndex == 0)
            {
                File.Copy($"data\\voices\\{_currentVoiceName}\\rvcmodelvoice.index", "C:\\rvcmodelvoice.index");
            }
            else
            {
                File.Copy($"data\\custom_index\\{guna2ComboBox7.SelectedItem}.index", "C:\\rvcmodelvoice.index");
            }
        }
    }

    private void RunFFMpeg(string arguments)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg.exe",
            Arguments = $"-threads {Environment.ProcessorCount} {arguments}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        }).WaitForExit();
    }

    private void guna2Button4_Click(object sender, EventArgs e)
    {
        openFileDialog1.FileName = "";
        saveFileDialog1.FileName = "";

        if (openFileDialog1.ShowDialog().Equals(DialogResult.OK) && saveFileDialog1.ShowDialog().Equals(DialogResult.OK))
        {
            if (File.Exists(saveFileDialog1.FileName))
            {
                File.Delete(saveFileDialog1.FileName);
            }

            CompressAudioFile(openFileDialog1.FileName, "C:\\input.wav");
            RunInference("C:\\input.wav", "C:\\output.wav");
            CompressAudioFile("C:\\output.wav", saveFileDialog1.FileName);
            DeleteTempFiles();
            MessageBox.Show("Succesfully generated your inferenced audio! Enjoy!", "PickAnyVoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void guna2Button5_Click(object sender, EventArgs e)
    {
        if (guna2Button5.Text == "Hear the original sample")
        {
            guna2Button5.Text = "Stop hearing the current sample";
            PlayWavAudioFile(Path.GetFullPath($"data\\exampleaudio.wav"));
        }
        else
        {
            StopPlayingAudio();
            guna2Button5.Text = "Hear the original sample";
        }
    }

    private void guna2Button6_Click(object sender, EventArgs e)
    {
        openFileDialog1.FileName = "";
        saveFileDialog1.FileName = "";

        if (guna2Button6.Text == "Start recording microphone for inference")
        {
            guna2Button6.Text = "Stop recording";

            if (File.Exists("C:\\recorded.wav"))
            {
                File.Delete("C:\\recorded.wav");
            }

            _waveIn = new WaveIn();
            _waveIn.DeviceNumber = guna2ComboBox1.SelectedIndex;
            _waveIn.WaveFormat = new WaveFormat(48000, 16, 1);
            _waveFileWriter = new WaveFileWriter("C:\\recorded.wav", _waveIn.WaveFormat);

            _waveIn.DataAvailable += (s, e1) =>
            {
                _waveFileWriter.Write(e1.Buffer, 0, e1.BytesRecorded);
                _waveFileWriter.Flush();
            };

            _waveIn.StartRecording();
        }
        else
        {
            guna2Button6.Text = "Start recording microphone for inference";

            _waveIn.StopRecording();
            _waveIn.Dispose();

            _waveFileWriter.Close();
            _waveFileWriter.Dispose();

            if (saveFileDialog1.ShowDialog().Equals(DialogResult.OK))
            {
                if (File.Exists(saveFileDialog1.FileName))
                {
                    File.Delete(saveFileDialog1.FileName);
                }

                CompressAudioFile("C:\\recorded.wav", "C:\\input.wav");
                RunInference("C:\\input.wav", "C:\\output.wav");
                CompressAudioFile("C:\\output.wav", saveFileDialog1.FileName);
                DeleteTempFiles();
                MessageBox.Show("Succesfully generated your inferenced audio by microphone recording! Enjoy!", "PickAnyVoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void guna2Button7_Click(object sender, EventArgs e)
    {
        if (guna2Button7.Text == "Start real-time voice changer")
        {
            guna2Button7.Text = "Stop real-time voice changer";

            string inputDevice = WaveIn.GetCapabilities(guna2ComboBox1.SelectedIndex).ProductName + " (MME)";
            string outputDevice = WaveOut.GetCapabilities(guna2ComboBox2.SelectedIndex).ProductName + " (MME)";

            for (int waveOutDevice = 0; waveOutDevice < WaveOut.DeviceCount; waveOutDevice++)
            {
                guna2ComboBox2.Items.Add(WaveOut.GetCapabilities(waveOutDevice).ProductName);
            }

            File.WriteAllText("data\\runtime\\values1.json", "{\"pth_path\": \"C:/rvcmodelvoice.pth\", \"index_path\": \"C:/rvcmodelvoice.index\", \"sg_input_device\": \"" + inputDevice + "\", \"sg_output_device\": \"" + outputDevice + "\", \"threhold\": -60.0, \"pitch\": " + guna2ComboBox3.SelectedItem.ToString() + ".0, \"index_rate\": 0.0, \"block_time\": 0.51, \"crossfade_length\": 0.15, \"extra_time\": 2.99, \"n_cpu\": 4.0, \"f0method\": \"rmvpe\"}");

            _process = new Process();
            _process.StartInfo.FileName = "gui_v2.bat";
            _process.StartInfo.WorkingDirectory = Path.GetFullPath("data\\runtime");
            _process.Start();

            bool hidden = false;

            while (!hidden)
            {
                List<WindowInfo> windows = GetWindows();

                foreach (WindowInfo info in windows)
                {
                    if (info.WindowText.Equals("RVC - GUI"))
                    {
                        ShowWindow(info.WindowHandle, 0);
                        ShowWindow(_process.MainWindowHandle, 0);
                        hidden = true;
                    }
                }
            }
        }
        else
        {
            _process.Kill();
            guna2Button7.Text = "Start real-time voice changer";

            List<WindowInfo> windows = GetWindows();

            foreach (WindowInfo info in windows)
            {
                if (info.WindowText.Equals("RVC - GUI"))
                {
                    info.CloseWindow();
                    info.DiagnosticsProcess.Kill();
                }
            }
        }
    }

    private List<WindowInfo> GetWindows()
    {
        List<WindowInfo> windows = new List<WindowInfo>();

        foreach (Process process in Process.GetProcesses())
        {
            foreach (ProcessThread thread in process.Threads)
            {
                EnumThreadWindows(thread.Id,
                (hWnd, lParam) =>
                {
                    try
                    {
                        windows.Add(new WindowInfo(hWnd, process, thread, (uint)process.Id, (uint)thread.Id));
                    }
                    catch
                    {

                    }

                    return true;
                },
                IntPtr.Zero);
            }
        }

        return windows;
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        try
        {
            _process.Kill();
        }
        catch
        {

        }

        List<WindowInfo> windows = GetWindows();

        foreach (WindowInfo info in windows)
        {
            if (info.WindowText.Equals("RVC - GUI"))
            {
                info.CloseWindow();
                info.DiagnosticsProcess.Kill();
            }
        }

        DeleteTempFiles(true);
        CloseAllPythonInstances();
        Environment.Exit(0);
    }

    private void CloseAllPythonInstances()
    {
        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == Process.GetCurrentProcess().Id)
                {
                    continue;
                }

                if (process.MainModule.ModuleName.ToLower().Contains("python") || process.ProcessName.ToLower().Contains("python")
                    || process.ProcessName.ToLower().Contains("ffmpeg") || process.ProcessName.ToLower().Contains("pickanyvoice")
                    || process.MainModule.FileName.ToLower().Contains("python")
                    || process.MainModule.FileName.ToLower().Contains("ffmpeg")
                    || process.MainModule.FileName.ToLower().Contains("pickanyvoice")
                    || process.ProcessName.ToLower().Equals("cmd"))
                {
                    process.Kill();
                }
            }
            catch
            {

            }
        }
    }

    private void PlayWavAudioFile(string path)
    {
        StopPlayingAudio();

        _waveOutEvent = new WaveOutEvent();
        _audioFileReader = new AudioFileReader(path);

        _waveOutEvent.Init(_audioFileReader);
        _waveOutEvent.Play();

        _waveOutEvent.PlaybackStopped += (sender, e) =>
        {
            _waveOutEvent.Dispose();
            _audioFileReader.Dispose();

            guna2Button2.Text = "Hear a sample of the picked voice";
            guna2Button5.Text = "Hear the original sample";
        };
    }

    private void StopPlayingAudio()
    {
        try
        {
            _waveOutEvent.Stop();
        }
        catch
        {

        }

        try
        {
            _audioFileReader.Close();
        }
        catch
        {

        }

        
        try
        {
            _waveOutEvent.Dispose();
        }
        catch
        {

        }

        try
        {
            _audioFileReader.Dispose();
        }
        catch
        {

        }
    }

    private void DeleteTempFiles(bool deleteModels = false)
    {
        foreach (string file in Directory.GetFiles("C:\\"))
        {
            string extension = Path.GetExtension(file).ToLower().Substring(1);

            if (deleteModels)
            {
                if (extension.Equals("index") || extension.Equals("pth"))
                {
                    File.Delete(file);
                }
            }

            if (extension.Equals("wav") || extension.Equals("mp3") || extension.Equals("txt"))
            {
                File.Delete(file);
            }
        }
        
        if (File.Exists("data\\runtime\\_do_infer_.txt"))
        {
            File.Delete("data\\runtime\\_do_infer_.txt");
        }

        if (File.Exists("data\\runtime\\finished.txt"))
        {
            File.Delete("data\\runtime\\finished.txt");
        }

        if (File.Exists("data\\tts\\finished.txt"))
        {
            File.Delete("data\\tts\\finished.txt");
        }

        if (File.Exists("data\\tts\\output.mp3"))
        {
            File.Delete("data\\tts\\output.mp3");
        }
    }

    private void RunInference(string inputAudioPath, string outputAudioPath)
    {
        File.WriteAllText("data\\runtime\\_do_infer_.txt", $"{inputAudioPath}\n{guna2ComboBox3.SelectedItem}\nC:\\rvcmodelvoice.index\nC:\\rvcmodelvoice.pth\n{outputAudioPath}");

        while (!File.Exists(outputAudioPath) && !File.Exists("C:\\finished.txt"))
        {
            Thread.Sleep(1);
        }

        File.Delete("C:\\finished.txt");
    }

    private void guna2ComboBox4_SelectedIndexChanged(object sender, EventArgs e)
    {
        guna2ComboBox5.Items.Clear();
        guna2ComboBox6.Items.Clear();

        if (guna2ComboBox4.SelectedIndex == 0)
        {
            foreach (EdgeTtsVoice voice in _edgeTtsVoices)
            {
                if (!guna2ComboBox5.Items.Contains(voice.VoiceLanguage))
                {
                    guna2ComboBox5.Items.Add(voice.VoiceLanguage);
                }
            }
        }
        else
        {
            foreach (GoogleTtsVoice voice in _googleTtsVoices)
            {
                if (!guna2ComboBox5.Items.Contains(voice.LanguageName))
                {
                    guna2ComboBox5.Items.Add(voice.LanguageName);
                }
            }
        }

        guna2ComboBox5.SelectedIndex = 0;
    }

    private void guna2CheckBox1_CheckedChanged(object sender, EventArgs e)
    {
        if (guna2CheckBox1.Checked)
        {
            guna2Button1.Enabled = _currentVoiceName != null;
        }
        else
        {
            guna2Button1.Enabled = true;
        }
    }

    private void guna2ComboBox5_SelectedIndexChanged(object sender, EventArgs e)
    {
        guna2ComboBox6.Items.Clear();

        if (guna2ComboBox4.SelectedIndex == 0)
        {
            foreach (EdgeTtsVoice voice in _edgeTtsVoices)
            {
                if (voice.VoiceLanguage.Equals(guna2ComboBox5.SelectedItem.ToString()))
                {
                    if (!guna2ComboBox6.Items.Contains(voice.VoiceName))
                    {
                        guna2ComboBox6.Items.Add(voice.VoiceName);
                    }
                }
            }

            guna2ComboBox6.SelectedIndex = 0;
        }
    }

    private void guna2Button8_Click(object sender, EventArgs e)
    {
        Process.Start("https://github.com/GabryB03/PickAnyVoice/");
    }

    private static IEnumerable<string> SplitToLines(string input)
    {
        if (input == null)
        {
            yield break;
        }

        using (System.IO.StringReader reader = new System.IO.StringReader(input))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }

    private void CompressAudioFile(string inputAudioPath, string outputAudioPath)
    {
        RunFFMpeg($"-i \"{inputAudioPath}\" -af aresample=osf=s16:dither_method=triangular_hp -sample_fmt s16 -ar 48000 -ac 1 -b:a 96k -acodec pcm_s16le -filter:a \"highpass=f=50, lowpass=f=15000\" -map a \"{outputAudioPath}\"");

        while (!File.Exists(outputAudioPath))
        {
            Thread.Sleep(1);
        }
    }

    private string TtsGetText()
    {
        List<string> lines = new List<string>();

        foreach (string line in SplitToLines(guna2TextBox2.Text))
        {
            if (line.Replace(" ", "").Replace('\t'.ToString(), "") == "")
            {
                continue;
            }

            lines.Add(line.Trim());
        }

        string completeText = "";

        foreach (string line in lines)
        {
            if (completeText == "")
            {
                completeText = line;
            }
            else
            {
                completeText += ". " + line;
            }
        }

        return completeText;
    }

    private string TtsGetVoice()
    {
        foreach (EdgeTtsVoice voice in _edgeTtsVoices)
        {
            if (guna2ComboBox5.SelectedItem.ToString().Equals(voice.VoiceLanguage) && guna2ComboBox6.SelectedItem.ToString().Equals(voice.VoiceName))
            {
                return voice.VoiceCompleteName;
            }
        }

        return "";
    }

    private string TtsGetLanguage()
    {
        foreach (GoogleTtsVoice voice in _googleTtsVoices)
        {
            if (guna2ComboBox5.SelectedItem.ToString().Equals(voice.LanguageName))
            {
                return voice.LanguageCode;
            }
        }

        return "";
    }

    private void guna2ComboBox7_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_currentVoiceName == "" || _currentVoiceName == null)
        {
            return;
        }

        File.Delete("C:\\rvcmodelvoice.index");

        if (guna2ComboBox7.SelectedIndex == 0)
        {
            File.Copy($"data\\voices\\{_currentVoiceName}\\rvcmodelvoice.index", "C:\\rvcmodelvoice.index");
        }
        else
        {
            File.Copy($"data\\custom_index\\{guna2ComboBox7.SelectedItem}.index", "C:\\rvcmodelvoice.index");
        }
    }

    private void guna2Button1_Click(object sender, EventArgs e)
    {
        DeleteTempFiles();

        openFileDialog1.FileName = "";
        saveFileDialog1.FileName = "";
        
        string text = TtsGetText();

        if (File.Exists("data\\tts\\finished.txt"))
        {
            File.Delete("data\\tts\\finished.txt");
        }

        if (File.Exists("data\\tts\\output.mp3"))
        {
            File.Delete("data\\tts\\output.mp3");
        }

        if (guna2ComboBox4.SelectedIndex == 0)
        {
            File.WriteAllBytes("data\\tts\\edgetts.py", Resources.infer_edge);
            string edgeTtsFile = File.ReadAllText("data\\tts\\edgetts.py");
            edgeTtsFile = edgeTtsFile.Replace("VALUE_TEXT", text).Replace("VALUE_VOICE", TtsGetVoice());
            File.WriteAllText("data\\tts\\edgetts.py", edgeTtsFile);
        }
        else
        {
            File.WriteAllBytes("data\\tts\\googletts.py", Resources.infer_google);
            string googleTtsFile = File.ReadAllText("data\\tts\\googletts.py");
            googleTtsFile = googleTtsFile.Replace("VALUE_TEXT", text).Replace("VALUE_LANGUAGE", TtsGetLanguage());
            File.WriteAllText("data\\tts\\googletts.py", googleTtsFile);
        }

        if (saveFileDialog1.ShowDialog().Equals(DialogResult.OK))
        {
            Process process = new Process();
            process.StartInfo.FileName = guna2ComboBox4.SelectedIndex == 0 ? "edgetts.bat" : "googletts.bat";
            process.StartInfo.WorkingDirectory = Path.GetFullPath("data\\tts");
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();

            while (!File.Exists("data\\tts\\finished.txt") && !File.Exists("data\\tts\\output.mp3"))
            {
                Thread.Sleep(1);
            }

            if (!guna2CheckBox1.Checked)
            {
                CompressAudioFile(Path.GetFullPath("data\\tts\\output.mp3"), saveFileDialog1.FileName);
                File.Delete("data\\tts\\finished.txt");
                File.Delete("data\\tts\\output.mp3");
            }
            else
            {
                CompressAudioFile(Path.GetFullPath("data\\tts\\output.mp3"), "C:\\input.wav");

                File.Delete("data\\tts\\finished.txt");
                File.Delete("data\\tts\\output.mp3");

                RunInference("C:\\input.wav", "C:\\output.wav");
                CompressAudioFile("C:\\output.wav", saveFileDialog1.FileName);
            }

            MessageBox.Show("Succesfully inferenced your text using TTS!", "PickAnyVoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}