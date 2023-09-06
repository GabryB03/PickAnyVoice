using MetroSuite;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

public partial class MainForm : MetroForm
{
    private List<string> _theVoices;
    private char[] _filteringCharacters = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private string _currentVoiceName;
    private WaveIn _waveIn;
    private WaveFileWriter _waveFileWriter;
    private Process _process;

    [DllImport("winmm.dll")]
    private static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

    private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public MainForm()
    {
        InitializeComponent();
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

        guna2ComboBox1.SelectedIndex = 0;
        guna2ComboBox2.SelectedIndex = 0;
        guna2ComboBox3.SelectedIndex = 12;
        guna2ComboBox4.SelectedIndex = 0;

        File.WriteAllBytes("data\\runtime\\infer_cli.py", PickAnyVoice.Properties.Resources.pythoninfer);
        File.WriteAllBytes("data\\runtime\\gui_v2.py", PickAnyVoice.Properties.Resources.V2PYTHON);
        File.WriteAllText("data\\runtime\\gui_v2.bat", PickAnyVoice.Properties.Resources.V2BAT);

        CloseAllPythonInstances();
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

    private void guna2Button1_Click(object sender, System.EventArgs e)
    {
        if (listBox1.SelectedItem == null)
        {
            return;
        }

        guna2Button1.Enabled = false;
        _currentVoiceName = listBox1.SelectedItem.ToString();
        guna2Button2.Enabled = true;
        guna2Button3.Enabled = true;
        guna2Button4.Enabled = true;
        guna2Button6.Enabled = true;
        guna2Button7.Enabled = true;
        guna2Button8.Enabled = true;
        label4.Text = _currentVoiceName;
        guna2CirclePictureBox1.Image = Image.FromFile($"data\\voices\\{_currentVoiceName}\\avatar.jpg");

        if (File.Exists("C:\\rvcmodelvoice.pth"))
        {
            File.Delete("C:\\rvcmodelvoice.pth");
        }

        if (File.Exists("C:\\rvcmodelvoice.index"))
        {
            File.Delete("C:\\rvcmodelvoice.index");
        }

        if (File.Exists("C:\\input.wav"))
        {
            File.Delete("C:\\input.wav");
        }

        if (File.Exists("C:\\output.wav"))
        {
            File.Delete("C:\\output.wav");
        }

        if (File.Exists("C:\\recorded.wav"))
        {
            File.Delete("C:\\recorded.wav");
        }

        File.Copy($"data\\voices\\{_currentVoiceName}\\rvcmodelvoice.pth", "C:\\rvcmodelvoice.pth");
        File.Copy($"data\\voices\\{_currentVoiceName}\\rvcmodelvoice.index", "C:\\rvcmodelvoice.index");
        MessageBox.Show($"Succesfully picked the voice of {_currentVoiceName}!", "PickAnyVoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void guna2Button2_Click(object sender, System.EventArgs e)
    {
        if (guna2Button2.Text == "Hear a sample of the picked voice")
        {
            guna2Button2.Text = "Stop hearing the current sample";
            mciSendString($"open \"{Path.GetFullPath($"data\\voices\\{_currentVoiceName}\\exampleaudio.wav")}\" alias exampleaudio", null, 0, IntPtr.Zero);
            mciSendString("play exampleaudio", null, 0, IntPtr.Zero);
        }
        else
        {
            mciSendString("stop exampleaudio", null, 0, IntPtr.Zero);
            mciSendString("close exampleaudio", null, 0, IntPtr.Zero);
            guna2Button2.Text = "Hear a sample of the picked voice";
        }
    }

    private void guna2Button3_Click(object sender, System.EventArgs e)
    {
        Process.Start($"data\\voices\\{_currentVoiceName}");
    }

    private void listBox1_SelectedIndexChanged(object sender, System.EventArgs e)
    {
        guna2Button1.Enabled = listBox1.SelectedItem != null && !listBox1.SelectedItem.ToString().Equals(_currentVoiceName);
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

            File.WriteAllText("data\\runtime\\infer-cli.bat", $"runtime\\python.exe infer_cli.py {guna2ComboBox3.SelectedItem.ToString()} \"C:\\input.wav\" \"C:\\output.wav\" \"C:\\rvcmodelvoice.pth\" \"C:\\rvcmodelvoice.index\" cuda:0 rmvpe\r\npause");
            RunFFMpeg($"-i \"{openFileDialog1.FileName}\" -af aresample=osf=s16:dither_method=triangular_hp -sample_fmt s16 -ar 48000 -ac 1 -b:a 96k -acodec pcm_s16le -filter:a \"highpass=f=50, lowpass=f=15000\" -map a \"C:\\input.wav\"");

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.WorkingDirectory = Path.GetFullPath("data\\runtime");
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine("infer-cli.bat");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();

            while (!File.Exists("C:\\output.wav"))
            {
                Thread.Sleep(1);
            }

            File.Delete("C:\\input.wav");
            RunFFMpeg($"-i \"C:\\output.wav\" -af aresample=osf=s16:dither_method=triangular_hp -sample_fmt s16 -ar 48000 -ac 1 -b:a 96k -acodec pcm_s16le -filter:a \"highpass=f=50, lowpass=f=15000\" -map a \"{saveFileDialog1.FileName}\"");
            File.Delete("C:\\output.wav");
            MessageBox.Show("Succesfully generated your inferenced audio! Enjoy!", "PickAnyVoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CloseAllPythonInstances();
        }
    }

    private void guna2Button5_Click(object sender, EventArgs e)
    {
        if (guna2Button5.Text == "Hear the original sample")
        {
            guna2Button5.Text = "Stop hearing the current sample";
            mciSendString($"open \"{Path.GetFullPath($"data\\exampleaudio.wav")}\" alias original", null, 0, IntPtr.Zero);
            mciSendString("play original", null, 0, IntPtr.Zero);
        }
        else
        {
            mciSendString("stop original", null, 0, IntPtr.Zero);
            mciSendString("close original", null, 0, IntPtr.Zero);
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

                if (File.Exists("C:\\input.wav"))
                {
                    File.Delete("C:\\input.wav");
                }

                if (File.Exists("C:\\output.wav"))
                {
                    File.Delete("C:\\output.wav");
                }

                File.WriteAllText("data\\runtime\\infer-cli.bat", $"runtime\\python.exe infer_cli.py {guna2ComboBox3.SelectedItem.ToString()} \"C:\\input.wav\" \"C:\\output.wav\" \"C:\\rvcmodelvoice.pth\" \"C:\\rvcmodelvoice.index\" cuda:0 rmvpe\r\npause");
                RunFFMpeg($"-i \"C:\\recorded.wav\" -af aresample=osf=s16:dither_method=triangular_hp -sample_fmt s16 -ar 48000 -ac 1 -b:a 96k -acodec pcm_s16le -filter:a \"highpass=f=50, lowpass=f=15000\" -map a \"C:\\input.wav\"");
                File.Delete("C:\\recorded.wav");

                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.WorkingDirectory = Path.GetFullPath("data\\runtime");
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.Start();

                cmd.StandardInput.WriteLine("infer-cli.bat");
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();
                cmd.WaitForExit();

                while (!File.Exists("C:\\output.wav"))
                {
                    Thread.Sleep(1);
                }

                File.Delete("C:\\input.wav");
                RunFFMpeg($"-i \"C:\\output.wav\" -af aresample=osf=s16:dither_method=triangular_hp -sample_fmt s16 -ar 48000 -ac 1 -b:a 96k -acodec pcm_s16le -filter:a \"highpass=f=50, lowpass=f=15000\" -map a \"{saveFileDialog1.FileName}\"");
                File.Delete("C:\\output.wav");
                MessageBox.Show("Succesfully generated your inferenced audio by microphone recording! Enjoy!", "PickAnyVoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CloseAllPythonInstances();
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

            CloseAllPythonInstances();
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

                if (process.MainModule.ModuleName.ToLower().Contains("python") || process.ProcessName.ToLower().Contains("python") || process.ProcessName.ToLower().Contains("ffmpeg") || process.ProcessName.ToLower().Contains("pickanyvoice"))
                {
                    process.Kill();
                }

                if (process.MainModule.FileName.ToLower().Contains("python") || process.MainModule.FileName.ToLower().Contains("ffmpeg") || process.MainModule.FileName.ToLower().Contains("pickanyvoice"))
                {
                    process.Kill();
                }
            }
            catch
            {

            }
        }
    }


    private byte[] ReadFully(Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }

    private void GenerateTTS(string language, string text, string outputPath)
    {
        if (File.Exists("C:\\temp.mp3"))
        {
            File.Delete("C:\\temp.mp3");
        }

        if (File.Exists("C:\\input.wav"))
        {
            File.Delete("C:\\input.wav");
        }

        if (File.Exists("C:\\output.wav"))
        {
            File.Delete("C:\\output.wav");
        }

        File.WriteAllText("data\\runtime\\infer-cli.bat", $"runtime\\python.exe infer_cli.py {guna2ComboBox3.SelectedItem.ToString()} \"C:\\input.wav\" \"C:\\output.wav\" \"C:\\rvcmodelvoice.pth\" \"C:\\rvcmodelvoice.index\" cuda:0 rmvpe\r\npause");
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://translate.google.com/translate_tts?ie=UTF-8&tl={language}&client=tw-ob&q={WebUtility.UrlEncode(text)}");
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        Stream stream = response.GetResponseStream();
        byte[] mp3Bytes = ReadFully(stream);
        response.Close();
        response.Dispose();
        stream.Close();
        stream.Dispose();
        File.WriteAllBytes("C:\\temp.mp3", mp3Bytes);
        RunFFMpeg($"-i \"C:\\temp.mp3\" -af aresample=osf=s16:dither_method=triangular_hp -sample_fmt s16 -ar 48000 -ac 1 -b:a 96k -acodec pcm_s16le -filter:a \"highpass=f=50, lowpass=f=15000\" -map a \"C:\\input.wav\"");
        File.Delete("C:\\temp.mp3");

        Process cmd = new Process();
        cmd.StartInfo.FileName = "cmd.exe";
        cmd.StartInfo.WorkingDirectory = Path.GetFullPath("data\\runtime");
        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.UseShellExecute = false;
        cmd.Start();

        cmd.StandardInput.WriteLine("infer-cli.bat");
        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        cmd.WaitForExit();

        while (!File.Exists("C:\\output.wav"))
        {
            Thread.Sleep(1);
        }

        File.Delete("C:\\input.wav");
        RunFFMpeg($"-i \"C:\\output.wav\" -af aresample=osf=s16:dither_method=triangular_hp -sample_fmt s16 -ar 48000 -ac 1 -b:a 96k -acodec pcm_s16le -filter:a \"highpass=f=50, lowpass=f=15000\" -map a \"{outputPath}\"");
        File.Delete("C:\\output.wav");
        MessageBox.Show("Succesfully generated your speech by text with the picked voice! Enjoy!", "PickAnyVoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        CloseAllPythonInstances();
    }

    private void guna2Button8_Click(object sender, EventArgs e)
    {
        if (saveFileDialog1.ShowDialog().Equals(DialogResult.OK))
        {
            if (File.Exists(saveFileDialog1.FileName))
            {
                File.Delete(saveFileDialog1.FileName);
            }

            string language = "en-EN";

            switch (guna2ComboBox4.SelectedIndex)
            {
                case 0:
                    language = "en-EN";
                    break;
                case 1:
                    language = "it-IT";
                    break;
                case 2:
                    language = "es-ES";
                    break;
                case 3:
                    language = "de-DE";
                    break;
                case 4:
                    language = "jp-JP";
                    break;
                case 5:
                    language = "ch-CH";
                    break;
                case 6:
                    language = "kr-KR";
                    break;
            }

            GenerateTTS(language, guna2TextBox2.Text, saveFileDialog1.FileName);
        }
    }
}