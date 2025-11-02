
namespace NAudioExample;

[App(icon: Icons.AudioLines, title: "NAudio")]
public class NAudioApp : ViewBase
{
    public override object? Build()
    {
        var frequencyState = UseState(100);
        var durationState = UseState(4);
        var volumeState = UseState(0.8f);

        var downloadUrl = this.UseDownload(() =>
        {
            var bytes = GenerateTone(frequencyState.Value, durationState.Value, volumeState.Value);

            return bytes;
        }, "audio/wav", $"tone_generated_by_naudio.wav");

        return Layout.Vertical().Gap(4).Padding(3).Width(Size.Units(200).Max(800))
               | new Card(Layout.Vertical().Gap(4).Padding(3)
                    | Text.H2("NAudio Demo")
                    | Text.Muted("NAudio is a library for audio processing. Fill in the form and click generate to download a custom tone.")
                    | new Separator()
                    | (Layout.Vertical().Padding(5).Width(Size.Full())
                        | Text.Label("Frequency")
                        | new NumberInput<int>(frequencyState)
                            .Min(50)
                            .Max(1000)
                            .Variant(NumberInputs.Slider)

                        | Text.Label("Duration")
                        | new NumberInput<int>(durationState)
                            .Min(1)
                            .Max(10)
                            .Variant(NumberInputs.Slider)

                        | Text.Label("Volume")
                        | new NumberInput<float>(volumeState)
                            .Min(0)
                            .Max(1)
                            .Step(0.01)
                            .Variant(NumberInputs.Slider)

                        | new Separator()
                        | (Layout.Horizontal().Width(Size.Full())
                            | new Button("Generate & Download").Url(downloadUrl.Value).Icon(Icons.AudioLines).Primary())
                    )
               );
    }

    private static byte[] GenerateTone(int frequency, int durationSeconds = 2, float volume = 0.2f)
    {
        try
        {
            var waveFormat = new WaveFormat(44100, 16, 1);

            var signalGenerator = new SignalGenerator()
            {
                Type = SignalGeneratorType.Sin,
                Frequency = frequency,
                Gain = volume
            }.Take(TimeSpan.FromSeconds(durationSeconds));

            using (var outputStream = new MemoryStream())
            {
                var waveProvider = new SampleToWaveProvider16(signalGenerator);

                using (var writer = new WaveFileWriter(outputStream, waveFormat))
                {
                    int totalBytes = waveFormat.AverageBytesPerSecond * durationSeconds;
                    byte[] buffer = new byte[totalBytes];
                    waveProvider.Read(buffer, 0, totalBytes);
                    writer.Write(buffer, 0, totalBytes);
                }

                return outputStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GenerateTone] ERROR: {ex.Message}");
            return Array.Empty<byte>();
        }
    }
}