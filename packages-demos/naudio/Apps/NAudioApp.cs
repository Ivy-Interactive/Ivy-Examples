namespace NAudioExample;

[App(icon: Icons.AudioLines, title: "NAudio")]
public class NAudioApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        try { MediaFoundationApi.Startup(); } catch { }

        // States for generation
        var freq = UseState(440);
        var dur = UseState(4.0);
        var vol = UseState(0.8f);
        var waveType = UseState(SignalGeneratorType.Sin);
        var genBytes = UseState<byte[]?>(() => null);
        var genDataUrl = UseState<string?>(() => null);
        var genError = UseState<string?>(() => null);
        var genVersion = UseState(() => 0);

        // States for upload
        var uploadBytes = UseState<byte[]?>(() => null);
        var uploadName = UseState<string?>(() => null);
        var format = UseState<WaveFormat?>(() => null);
        var duration = UseState<TimeSpan?>(() => null);

        // States for mixing
        var mixGenVol = UseState(0.5f);
        var mixUploadVol = UseState(0.5f);
        var mixBytes = UseState<byte[]?>(() => null);
        var mixDataUrl = UseState<string?>(() => null);
        var mixError = UseState<string?>(() => null);
        var mixVersion = UseState(() => 0);

        // File upload handler
        var uploadedFile = UseState<FileUpload<byte[]>?>(() => null);
        var uploadUrl = this.UseUpload(MemoryStreamUploadHandler.Create(uploadedFile))
            .Accept("audio/*")
            .MaxFileSize(50 * 1024 * 1024);

        // Process uploaded file when available
        UseEffect(() =>
        {
            if (uploadedFile.Value?.Content is byte[] bytes && bytes.Length > 0)
            {
                try
                {
                    var fileName = uploadedFile.Value.FileName ?? "uploaded_audio";
                    uploadBytes.Set(bytes);
                    uploadName.Set(fileName);
                    client.Toast($"File '{fileName}' received, processing...");

                    Task.Run(() =>
                    {
                        try
                        {
                            var info = GetAudioInfo(bytes);
                            if (info.HasValue)
                            {
                                format.Set(info.Value.WaveFormat);
                                duration.Set(info.Value.Duration);
                                if (info.Value.Duration.HasValue)
                                {
                                    dur.Set(info.Value.Duration.Value.TotalSeconds);
                                }
                                client.Toast($"Audio file '{fileName}' loaded successfully");
                            }
                            else
                            {
                                client.Toast($"File uploaded but could not read audio format", "Warning");
                            }
                        }
                        catch (Exception ex)
                        {
                            client.Toast($"Error processing audio: {ex.Message}", "Error");
                        }
                    });
                }
                catch (Exception ex)
                {
                    client.Toast($"Error uploading: {ex.Message}", "Error");
                    uploadBytes.Set((byte[]?)null);
                    uploadName.Set((string?)null);
                }
            }
        }, [uploadedFile]);

        // Download URLs
        var genUrl = this.UseDownload(() => genBytes.Value ?? Array.Empty<byte>(), "audio/wav", $"generated_tone_{genVersion.Value}.wav");
        var mixUrl = this.UseDownload(() => mixBytes.Value ?? Array.Empty<byte>(), "audio/wav", $"mixed_audio_{mixVersion.Value}.wav");

        return Layout.Vertical().Align(Align.TopCenter)
            | new Card(
                Layout.Vertical()
                | Text.H1("NAudio")
                | Text.Muted("Upload audio files, generate custom tones with adjustable parameters, and mix them together. Perfect for audio experimentation and sound design.")

                // Section 1: Upload File
                | Text.H3("Upload File")
                | (Layout.Vertical()
                    | uploadedFile.ToFileInput(uploadUrl).Placeholder("Choose Audio File")
                    | (uploadBytes.Value != null && uploadName.Value != null
                        ? new Callout($"File loaded: {uploadName.Value}\n" +
                            $"Format: {format.Value?.SampleRate}Hz, {format.Value?.Channels} channel(s), {format.Value?.BitsPerSample} bits\n" +
                            $"Duration: {duration.Value?.TotalSeconds:F2} seconds", variant: CalloutVariant.Info)
                        : Callout.Info("No file uploaded"))
                )

                // Section 2: Generate Sound
                | Text.H3("Generate Sound")
                | (Layout.Vertical()
                    | Text.Label("Wave Type")
                    | waveType.ToSelectInput(typeof(SignalGeneratorType).ToOptions())
                    | Text.Label("Frequency (Hz)")
                    | new NumberInput<int>(freq).Min(50).Max(1000).Variant(NumberInputs.Slider)
                    | Text.Label("Duration (seconds)")
                    | new NumberInput<double>(dur).Min(0.1).Max(600).Step(0.1).Variant(NumberInputs.Slider)
                    | Text.Label("Volume")
                    | new NumberInput<float>(vol).Min(0).Max(1).Step(0.01).Variant(NumberInputs.Slider)
                    | (genError.Value != null ? new Callout(genError.Value, variant: CalloutVariant.Error) : null)
                    | new Button("Generate").Primary().Icon(Icons.Play).HandleClick(_ =>
                    {
                        try
                        {
                            genError.Set((string?)null);
                            genBytes.Set((byte[]?)null);
                            genVersion.Set(genVersion.Value + 1);
                            var bytes = GenerateTone(freq.Value, dur.Value, vol.Value, waveType.Value);
                            genBytes.Set(bytes);
                            genDataUrl.Set("data:audio/wav;base64," + Convert.ToBase64String(bytes));
                            client.Toast("Sound generated successfully");
                        }
                        catch (Exception ex)
                        {
                            genError.Set(ex.Message);
                            genBytes.Set((byte[]?)null);
                            genDataUrl.Set((string?)null);
                        }
                    }).Width(Size.Full())
                    | (!string.IsNullOrEmpty(genDataUrl.Value)
                        ? Layout.Vertical().Gap(2)
                            | Text.Small("Generated Audio")
                            | new Audio(genDataUrl.Value)
                                .Controls(true)
                                .Key($"audio-gen-{genVersion.Value}")
                        : null!)
                )

                // Section 3: Mix Audio
                | Text.H3("Mix Audio")
                | (Layout.Vertical()
                    | (genBytes.Value == null
                        ? new Callout("Generate a sound first", variant: CalloutVariant.Warning)
                        : Text.Muted($"Generated sound ready ({genBytes.Value.Length / 1024} KB)"))
                    | (uploadBytes.Value == null
                        ? new Callout("Upload a file first", variant: CalloutVariant.Warning)
                        : Text.Muted($"Uploaded file ready ({uploadBytes.Value.Length / 1024} KB)"))
                    | Text.Label("Generated Sound Volume")
                    | new NumberInput<float>(mixGenVol).Min(0).Max(1).Step(0.01).Variant(NumberInputs.Slider)
                    | Text.Label("Uploaded File Volume")
                    | new NumberInput<float>(mixUploadVol).Min(0).Max(1).Step(0.01).Variant(NumberInputs.Slider)
                    | (mixError.Value != null ? new Callout(mixError.Value, variant: CalloutVariant.Error) : null)
                    | new Button("Mix").Primary().Icon(Icons.Layers).HandleClick(_ =>
                    {
                        if (genBytes.Value == null || uploadBytes.Value == null)
                        { mixError.Set("Both generated sound and uploaded file are required"); return; }
                        try
                        {
                            mixError.Set((string?)null);
                            mixBytes.Set((byte[]?)null);
                            mixVersion.Set(mixVersion.Value + 1);
                            var mixed = MixAudio(genBytes.Value, uploadBytes.Value, mixGenVol.Value, mixUploadVol.Value);
                            mixBytes.Set(mixed);
                            mixDataUrl.Set("data:audio/wav;base64," + Convert.ToBase64String(mixed));
                            client.Toast("Audio mixed successfully");
                        }
                        catch (Exception ex) 
                        { 
                            mixError.Set(ex.Message); 
                            mixBytes.Set((byte[]?)null);
                            mixDataUrl.Set((string?)null);
                        }
                    }).Disabled(genBytes.Value == null || uploadBytes.Value == null).Width(Size.Full())
                    | (!string.IsNullOrEmpty(mixDataUrl.Value)
                        ? Layout.Vertical().Gap(2)
                            | Text.Small("Mixed Audio")
                            | new Audio(mixDataUrl.Value)
                                .Controls(true)
                                .Key($"audio-mix-{mixVersion.Value}")
                        : null!)
                    | new Spacer().Height(Size.Units(10))
                    | Text.Small("This demo uses NAudio library for generating and mixing audio.")
                    | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [NAudio](https://github.com/naudio/NAudio)")
                )
            ).Width(Size.Units(200).Max(Size.Full()));
    }

    private static ISampleProvider LoadAudio(string filePath)
    {
        try { return new AudioFileReader(filePath).ToSampleProvider(); }
        catch
        {
            try { return new WaveFileReader(filePath).ToSampleProvider(); }
            catch { return new MediaFoundationReader(filePath).ToSampleProvider(); }
        }
    }

    private (WaveFormat WaveFormat, TimeSpan? Duration)? GetAudioInfo(byte[] audioBytes)
    {
        if (audioBytes == null || audioBytes.Length == 0) return null;
        string? tempFile = null;
        try
        {
            tempFile = System.IO.Path.GetTempFileName();
            File.WriteAllBytes(tempFile, audioBytes);
            try
            {
                using var reader = new WaveFileReader(tempFile);
                return (reader.WaveFormat, reader.TotalTime);
            }
            catch
            {
                try
                {
                    using var reader = new AudioFileReader(tempFile);
                    return (reader.WaveFormat, reader.TotalTime);
                }
                catch
                {
                    using var reader = new MediaFoundationReader(tempFile);
                    return (reader.WaveFormat, reader.TotalTime);
                }
            }
        }
        catch { return null; }
        finally { if (tempFile != null && File.Exists(tempFile)) try { File.Delete(tempFile); } catch { } }
    }

    private static byte[] GenerateTone(int frequency, double durationSeconds, float volume, SignalGeneratorType waveType)
    {
        try
        {
            var waveFormat = new WaveFormat(44100, 16, 1);
            var signalGenerator = new SignalGenerator() { Type = waveType, Frequency = frequency, Gain = volume }
                .Take(TimeSpan.FromSeconds(durationSeconds));
            using var outputStream = new MemoryStream();
            var waveProvider = new SampleToWaveProvider16(signalGenerator);
            using (var writer = new WaveFileWriter(outputStream, waveFormat))
            {
                int totalBytes = (int)(waveFormat.AverageBytesPerSecond * durationSeconds);
                byte[] buffer = new byte[totalBytes];
                waveProvider.Read(buffer, 0, totalBytes);
                writer.Write(buffer, 0, totalBytes);
            }
            return outputStream.ToArray();
        }
        catch (Exception ex) { throw new Exception($"Error generating tone: {ex.Message}"); }
    }

    private byte[] MixAudio(byte[] generatedBytes, byte[] uploadedBytes, float generatedVolume, float uploadedVolume)
    {
        string? genTempFile = null, upTempFile = null;
        try
        {
            genTempFile = System.IO.Path.GetTempFileName();
            File.WriteAllBytes(genTempFile, generatedBytes);
            var genReader = new WaveFileReader(genTempFile);
            ISampleProvider generatedSource = new VolumeSampleProvider(genReader.ToSampleProvider()) { Volume = generatedVolume };

            upTempFile = System.IO.Path.GetTempFileName();
            File.WriteAllBytes(upTempFile, uploadedBytes);
            ISampleProvider uploadedSource = new VolumeSampleProvider(LoadAudio(upTempFile)) { Volume = uploadedVolume };

            // Determine target format: use highest sample rate and channels
            int targetSampleRate = Math.Max(generatedSource.WaveFormat.SampleRate, uploadedSource.WaveFormat.SampleRate);
            int targetChannels = Math.Max(generatedSource.WaveFormat.Channels, uploadedSource.WaveFormat.Channels);
            var targetFormat = WaveFormat.CreateIeeeFloatWaveFormat(targetSampleRate, targetChannels);

            // Resample generated audio if needed
            if (generatedSource.WaveFormat.SampleRate != targetSampleRate)
            {
                var genWaveFormat = new WaveFormat(targetSampleRate, generatedSource.WaveFormat.Channels);
                var waveProvider = generatedSource.ToWaveProvider16();
                var resampler = new MediaFoundationResampler(waveProvider, genWaveFormat);
                generatedSource = resampler.ToSampleProvider();
            }

            // Resample uploaded audio if needed
            if (uploadedSource.WaveFormat.SampleRate != targetSampleRate)
            {
                var upWaveFormat = new WaveFormat(targetSampleRate, uploadedSource.WaveFormat.Channels);
                var waveProvider = uploadedSource.ToWaveProvider16();
                var resampler = new MediaFoundationResampler(waveProvider, upWaveFormat);
                uploadedSource = resampler.ToSampleProvider();
            }

            // Convert channels if needed
            if (generatedSource.WaveFormat.Channels == 1 && targetChannels == 2)
                generatedSource = new MonoToStereoSampleProvider(generatedSource);
            else if (generatedSource.WaveFormat.Channels == 2 && targetChannels == 1)
                generatedSource = new StereoToMonoSampleProvider(generatedSource);

            if (uploadedSource.WaveFormat.Channels == 1 && targetChannels == 2)
                uploadedSource = new MonoToStereoSampleProvider(uploadedSource);
            else if (uploadedSource.WaveFormat.Channels == 2 && targetChannels == 1)
                uploadedSource = new StereoToMonoSampleProvider(uploadedSource);

            var mixer = new MixingSampleProvider(targetFormat);
            mixer.AddMixerInput(generatedSource);
            mixer.AddMixerInput(uploadedSource);

            using var outputStream = new MemoryStream();
            using var writer = new WaveFileWriter(outputStream, targetFormat);
            var buffer = new float[targetFormat.SampleRate * targetFormat.Channels];
            int samplesRead;
            while ((samplesRead = mixer.Read(buffer, 0, buffer.Length)) > 0)
                writer.WriteSamples(buffer, 0, samplesRead);
            return outputStream.ToArray();
        }
        catch (Exception ex) { throw new Exception($"Error mixing audio: {ex.Message}"); }
        finally
        {
            if (genTempFile != null && File.Exists(genTempFile)) try { File.Delete(genTempFile); } catch { }
            if (upTempFile != null && File.Exists(upTempFile)) try { File.Delete(upTempFile); } catch { }
        }
    }

}
