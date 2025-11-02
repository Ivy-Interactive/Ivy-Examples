namespace NAudioExample;

[App(icon: Icons.AudioLines, title: "NAudio")]
public class NAudioApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        try { MediaFoundationApi.Startup(); } catch { }

        // States
        var freq = UseState(440);
        var dur = UseState(4);
        var vol = UseState(0.8f);
        var waveType = UseState(SignalGeneratorType.Sin);
        var genBytes = UseState<byte[]?>(() => null);
        var genError = UseState<string?>(() => null);
        var genVersion = UseState(() => 0); // Version counter to force URL refresh

        var uploadBytes = UseState<byte[]?>(() => null);
        var uploadName = UseState<string?>(() => null);
        var format = UseState<WaveFormat?>(() => null);
        var duration = UseState<TimeSpan?>(() => null);
        
        var trimStart = UseState(0.0);
        var trimDur = UseState<double?>(() => null);
        var procVol = UseState(1.0f);
        var fadeIn = UseState(0.0);
        var fadeOut = UseState(0.0);
        var procBytes = UseState<byte[]?>(() => null);
        var procError = UseState<string?>(() => null);

        var mixGenVol = UseState(0.5f);
        var mixUploadVol = UseState(0.5f);
        var mixBytes = UseState<byte[]?>(() => null);
        var mixError = UseState<string?>(() => null);
        var mixVersion = UseState(() => 0);

        var vizData = UseState<string?>(() => null);
        var vizError = UseState<string?>(() => null);

        var targetFmt = UseState<string>("WAV");
        var convBytes = UseState<byte[]?>(() => null);
        var convError = UseState<string?>(() => null);

        var fileInput = UseState<FileInput?>(() => null);
        var uploadUrl = this.UseUpload(uploadedBytes =>
        {
            try
            {
                var fileName = fileInput.Value?.Name ?? "uploaded_audio";
                uploadBytes.Set(uploadedBytes);
                uploadName.Set(fileName);
                client.Toast($"File '{fileName}' received, processing...");
                
                Task.Run(() =>
                {
                    try
                    {
                        var info = GetAudioInfo(uploadedBytes);
                        if (info.HasValue)
                        {
                            format.Set(info.Value.WaveFormat);
                            duration.Set(info.Value.Duration);
                            if (info.Value.Duration.HasValue)
                                trimDur.Set((double?)info.Value.Duration.Value.TotalSeconds);
                            client.Toast($"Audio file '{fileName}' loaded successfully");
                        }
                        else
                            client.Toast($"File uploaded but could not read audio format", "Warning");
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
        }, "audio/*", "audio-upload");

        // URL for audio playback and download - include version in filename to prevent caching
        var genUrl = this.UseDownload(() => genBytes.Value ?? Array.Empty<byte>(), "audio/wav", $"generated_tone_{genVersion.Value}.wav");
        var procUrl = this.UseDownload(() => procBytes.Value ?? Array.Empty<byte>(), "audio/wav", $"processed_{uploadName.Value ?? "audio"}.wav");
        var mixUrl = this.UseDownload(() => mixBytes.Value ?? Array.Empty<byte>(), "audio/wav", $"mixed_audio_{mixVersion.Value}.wav");
        var convUrl = this.UseDownload(() => convBytes.Value ?? Array.Empty<byte>(),
            targetFmt.Value == "WAV" ? "audio/wav" : "audio/mpeg",
            $"converted_{uploadName.Value ?? "audio"}.{(targetFmt.Value == "WAV" ? "wav" : "mp3")}");

        object BuildToneTab() => Layout.Vertical().Gap(4).Padding(3)
            | Text.H3("Generate Audio Tone")
            | Text.Muted("Generate a custom tone with adjustable parameters")
            | new Separator()
            | (Layout.Vertical().Gap(3).Padding(3)
                | Text.Label("Wave Type")
                | waveType.ToSelectInput(typeof(SignalGeneratorType).ToOptions())
                | Text.Label("Frequency (Hz)")
                | new NumberInput<int>(freq).Min(50).Max(1000).Variant(NumberInputs.Slider)
                | Text.Label("Duration (seconds)")
                | new NumberInput<int>(dur).Min(1).Max(10).Variant(NumberInputs.Slider)
                | Text.Label("Volume")
                | new NumberInput<float>(vol).Min(0).Max(1).Step(0.01).Variant(NumberInputs.Slider)
                | new Separator()
                | (genError.Value != null ? new Callout(genError.Value, variant: CalloutVariant.Error) : null)
                | new Button("Generate").Primary().Icon(Icons.Play).HandleClick(_ =>
                {
                    try
                    {
                        genError.Set((string?)null);
                        genBytes.Set((byte[]?)null); // Clear first to force widget removal
                        genVersion.Set(genVersion.Value + 1); // Increment version to get new URL
                        genBytes.Set(GenerateTone(freq.Value, dur.Value, vol.Value, waveType.Value));
                        client.Toast("Tone generated successfully");
                    }
                    catch (Exception ex)
                    {
                        genError.Set(ex.Message);
                        genBytes.Set((byte[]?)null);
                    }
                }).Width(Size.Full())
                | (genBytes.Value != null
                    ? Layout.Vertical().Gap(2)
                        | Text.Small("Generated Audio")
                        | new Audio(this.UseDownload(() => genBytes.Value ?? Array.Empty<byte>(), "audio/wav", $"generated_tone_{genVersion.Value}.wav").Value)
                            .Controls(true)
                            .Key($"audio-gen-{genVersion.Value}")
                    : null!)
            );

        object BuildUploadTab() => Layout.Vertical().Gap(4).Padding(3)
            | Text.H3("Upload & Process Audio")
            | Text.Muted("Upload an audio file and apply various effects")
            | new Separator()
            | (Layout.Vertical().Gap(3).Padding(3)
                | Text.Label("Upload Audio File")
                | fileInput.ToFileInput(uploadUrl, "Choose Audio File").Accept("audio/*")
                | (uploadBytes.Value != null && uploadName.Value != null
                    ? new Callout($"File loaded: {uploadName.Value}\n" +
                        $"Format: {format.Value?.SampleRate}Hz, {format.Value?.Channels} channel(s), {format.Value?.BitsPerSample} bits\n" +
                        $"Duration: {duration.Value?.TotalSeconds:F2} seconds", variant: CalloutVariant.Info)
                    : null)
                | new Separator()
                | Text.Label("Trim Audio")
                | (Layout.Horizontal().Gap(2)
                    | (Layout.Vertical().Gap(1)
                        | Text.Small("Start (seconds)")
                        | new NumberInput<double>(trimStart).Min(0).Max(duration.Value?.TotalSeconds ?? 10).Step(0.1))
                    | (Layout.Vertical().Gap(1)
                        | Text.Small("Duration (seconds)")
                        | new NumberInput<double?>(trimDur).Min(0.1).Max((duration.Value?.TotalSeconds ?? 10) - trimStart.Value).Step(0.1)))
                | Text.Label("Volume")
                | new NumberInput<float>(procVol).Min(0).Max(2).Step(0.01).Variant(NumberInputs.Slider)
                | Text.Label("Fade In (seconds)")
                | new NumberInput<double>(fadeIn).Min(0).Max(10).Step(0.1).Variant(NumberInputs.Slider)
                | Text.Label("Fade Out (seconds)")
                | new NumberInput<double>(fadeOut).Min(0).Max(10).Step(0.1).Variant(NumberInputs.Slider)
                | new Separator()
                | (procError.Value != null ? new Callout(procError.Value, variant: CalloutVariant.Error) : null)
                | (Layout.Horizontal().Gap(2).Width(Size.Full())
                    | new Button("Process").Primary().Icon(Icons.Settings).HandleClick(_ =>
                    {
                        if (uploadBytes.Value == null) { procError.Set("Please upload an audio file first"); return; }
                        try
                        {
                            procError.Set((string?)null);
                            procBytes.Set(ProcessAudio(uploadBytes.Value, trimStart.Value, trimDur.Value, procVol.Value, fadeIn.Value, fadeOut.Value));
                            client.Toast("Audio processed successfully");
                        }
                        catch (Exception ex) { procError.Set(ex.Message); procBytes.Set((byte[]?)null); }
                    }).Disabled(uploadBytes.Value == null)
                    | new Button("Download").Icon(Icons.Download).Url(procUrl.Value).Disabled(procBytes.Value == null)
                )
            );

        object BuildMixTab() => Layout.Vertical().Gap(4).Padding(3)
            | Text.H3("Mix Audio")
            | Text.Muted("Combine generated tone with uploaded audio")
            | new Separator()
            | (Layout.Vertical().Gap(3).Padding(3)
                | (genBytes.Value == null
                    ? new Callout("Generate a tone in the 'Tone Generation' tab first", variant: CalloutVariant.Warning)
                    : Text.Small($"Generated tone ready ({genBytes.Value.Length / 1024} KB)"))
                | (uploadBytes.Value == null
                    ? new Callout("Upload an audio file in the 'Upload & Process' tab first", variant: CalloutVariant.Warning)
                    : Text.Small($"Uploaded audio ready ({uploadBytes.Value.Length / 1024} KB)"))
                | new Separator()
                | Text.Label("Generated Tone Volume")
                | new NumberInput<float>(mixGenVol).Min(0).Max(1).Step(0.01).Variant(NumberInputs.Slider)
                | Text.Label("Uploaded Audio Volume")
                | new NumberInput<float>(mixUploadVol).Min(0).Max(1).Step(0.01).Variant(NumberInputs.Slider)
                | new Separator()
                | (mixError.Value != null ? new Callout(mixError.Value, variant: CalloutVariant.Error) : null)
                | new Button("Mix").Primary().Icon(Icons.Layers).HandleClick(_ =>
                {
                    if (genBytes.Value == null || uploadBytes.Value == null)
                    { mixError.Set("Both generated tone and uploaded audio are required"); return; }
                    try
                    {
                        mixError.Set((string?)null);
                        mixBytes.Set((byte[]?)null); // Clear first to force widget removal
                        mixVersion.Set(mixVersion.Value + 1); // Increment version to get new URL
                        mixBytes.Set(MixAudio(genBytes.Value, uploadBytes.Value, mixGenVol.Value, mixUploadVol.Value));
                        client.Toast("Audio mixed successfully");
                    }
                    catch (Exception ex) 
                    { 
                        mixError.Set(ex.Message); 
                        mixBytes.Set((byte[]?)null);
                    }
                }).Disabled(genBytes.Value == null || uploadBytes.Value == null).Width(Size.Full())
                | (mixBytes.Value != null
                    ? Layout.Vertical().Gap(2)
                        | Text.Small("Mixed Audio")
                        | new Audio(mixUrl.Value)
                            .Controls(true)
                    : null!)
            );

        object BuildVizTab() => Layout.Vertical().Gap(4).Padding(3)
            | Text.H3("Visualize Audio Waveform")
            | Text.Muted("View waveform visualization of audio")
            | new Separator()
            | (Layout.Vertical().Gap(3).Padding(3)
                | Text.Label("Select Audio Source")
                | (Layout.Horizontal().Gap(2)
                    | new Button("Visualize Generated").HandleClick(_ =>
                    {
                        if (genBytes.Value == null) { vizError.Set("No generated audio available"); return; }
                        try { vizError.Set((string?)null); vizData.Set(VisualizeWaveform(genBytes.Value)); }
                        catch (Exception ex) { vizError.Set(ex.Message); vizData.Set((string?)null); }
                    }).Disabled(genBytes.Value == null)
                    | new Button("Visualize Uploaded").HandleClick(_ =>
                    {
                        if (uploadBytes.Value == null) { vizError.Set("No uploaded audio available"); return; }
                        try { vizError.Set((string?)null); vizData.Set(VisualizeWaveform(uploadBytes.Value)); }
                        catch (Exception ex) { vizError.Set(ex.Message); vizData.Set((string?)null); }
                    }).Disabled(uploadBytes.Value == null))
                | (vizError.Value != null ? new Callout(vizError.Value, variant: CalloutVariant.Error) : null)
                | (vizData.Value != null
                    ? vizData.ToCodeInput().Language(Languages.Text).Height(Size.Units(400))
                    : Text.Muted("Select an audio source to visualize"))
            );

        object BuildConvertTab() => Layout.Vertical().Gap(4).Padding(3)
            | Text.H3("Convert Audio Format")
            | Text.Muted("Convert between WAV and MP3 formats")
            | new Separator()
            | (Layout.Vertical().Gap(3).Padding(3)
                | (uploadBytes.Value == null
                    ? new Callout("Upload an audio file in the 'Upload & Process' tab first", variant: CalloutVariant.Warning)
                    : Text.Small($"Ready to convert: {uploadName.Value}"))
                | Text.Label("Target Format")
                | targetFmt.ToSelectInput(new[] { new Option<string>("WAV", "WAV"), new Option<string>("MP3", "MP3") })
                | new Separator()
                | (convError.Value != null ? new Callout(convError.Value, variant: CalloutVariant.Error) : null)
                | (Layout.Horizontal().Gap(2).Width(Size.Full())
                    | new Button("Convert & Download").Primary().Icon(Icons.Download).HandleClick(_ =>
                    {
                        if (uploadBytes.Value == null) { convError.Set("Please upload an audio file first"); return; }
                        try
                        {
                            convError.Set((string?)null);
                            convBytes.Set(ConvertAudioFormat(uploadBytes.Value, targetFmt.Value));
                            client.Toast($"Audio converted to {targetFmt.Value} successfully");
                        }
                        catch (Exception ex) { convError.Set(ex.Message); convBytes.Set((byte[]?)null); }
                    }).Disabled(uploadBytes.Value == null)
                    | new Button("Download").Icon(Icons.Download).Url(convUrl.Value).Disabled(convBytes.Value == null)
                )
            );

        return Layout.Vertical().Gap(4).Padding(3).Width(Size.Units(200).Max(1000))
            | new Card(
                Layout.Vertical().Gap(4).Padding(3)
                | Text.H2("NAudio Demo")
                | Text.Muted("Comprehensive audio processing with NAudio library")
                | new Separator()
                | Layout.Tabs(
                    new Tab("Tone Generation", BuildToneTab()).Icon(Icons.Play),
                    new Tab("Upload & Process", BuildUploadTab()).Icon(Icons.Upload),
                    new Tab("Mixing", BuildMixTab()).Icon(Icons.Layers),
                    new Tab("Visualization", BuildVizTab()).Icon(Icons.ChartLine),
                    new Tab("Convert Format", BuildConvertTab()).Icon(Icons.FileCode)
                ).Variant(TabsVariant.Tabs)
            );
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

    private static byte[] GenerateTone(int frequency, int durationSeconds, float volume, SignalGeneratorType waveType)
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
                int totalBytes = waveFormat.AverageBytesPerSecond * durationSeconds;
                byte[] buffer = new byte[totalBytes];
                waveProvider.Read(buffer, 0, totalBytes);
                writer.Write(buffer, 0, totalBytes);
            }
            return outputStream.ToArray();
        }
        catch (Exception ex) { throw new Exception($"Error generating tone: {ex.Message}"); }
    }

    private byte[] ProcessAudio(byte[] audioBytes, double trimStart, double? trimDuration, float volume, double fadeIn, double fadeOut)
    {
        string? tempFile = null;
        try
        {
            tempFile = System.IO.Path.GetTempFileName();
            File.WriteAllBytes(tempFile, audioBytes);
            var reader = new AudioFileReader(tempFile);
            var totalDuration = reader.TotalTime;
            var audioSource = reader.ToSampleProvider();

            if (trimStart > 0 || trimDuration.HasValue)
            {
                var offsetProvider = new OffsetSampleProvider(audioSource) { SkipOver = TimeSpan.FromSeconds(trimStart) };
                if (trimDuration.HasValue) offsetProvider.Take = TimeSpan.FromSeconds(trimDuration.Value);
                audioSource = offsetProvider;
            }

            if (volume != 1.0f) audioSource = new VolumeSampleProvider(audioSource) { Volume = volume };

            var waveFormat = audioSource.WaveFormat;
            using var outputStream = new MemoryStream();
            using var writer = new WaveFileWriter(outputStream, waveFormat);

            var buffer = new float[waveFormat.SampleRate * waveFormat.Channels];
            int samplesRead;
            long totalSamples = 0;
            long fadeInEndSample = fadeIn > 0 ? (long)(fadeIn * waveFormat.SampleRate) : 0;
            long fadeOutStartSample = fadeOut > 0
                ? (long)((totalDuration.TotalSeconds - fadeOut) * waveFormat.SampleRate) : 0;

            while ((samplesRead = audioSource.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (fadeIn > 0 && totalSamples < fadeInEndSample)
                {
                    double fadeProgress = (double)totalSamples / (fadeIn * waveFormat.SampleRate);
                    float fadeMultiplier = (float)Math.Min(fadeProgress, 1.0);
                    for (int i = 0; i < samplesRead; i++) buffer[i] *= fadeMultiplier;
                }

                if (fadeOut > 0 && fadeOutStartSample > 0 && totalSamples >= fadeOutStartSample)
                {
                    double fadeProgress = (double)(totalSamples - fadeOutStartSample) / (fadeOut * waveFormat.SampleRate);
                    if (fadeProgress < 1.0)
                    {
                        float fadeMultiplier = (float)(1.0 - fadeProgress);
                        for (int i = 0; i < samplesRead; i++) buffer[i] *= fadeMultiplier;
                    }
                    else break;
                }

                writer.WriteSamples(buffer, 0, samplesRead);
                totalSamples += samplesRead;
            }
            return outputStream.ToArray();
        }
        catch (Exception ex) { throw new Exception($"Error processing audio: {ex.Message}"); }
        finally { if (tempFile != null && File.Exists(tempFile)) try { File.Delete(tempFile); } catch { } }
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

    private string VisualizeWaveform(byte[] audioBytes)
    {
        string? tempFile = null;
        try
        {
            tempFile = System.IO.Path.GetTempFileName();
            File.WriteAllBytes(tempFile, audioBytes);
            var reader = new AudioFileReader(tempFile);
            var totalDuration = reader.TotalTime;
            var audioSource = reader.ToSampleProvider();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Waveform Visualization");
            sb.AppendLine($"Format: {audioSource.WaveFormat.SampleRate}Hz, {audioSource.WaveFormat.Channels} channel(s), {audioSource.WaveFormat.BitsPerSample} bits");
            sb.AppendLine($"Duration: {totalDuration.TotalSeconds:F2} seconds");
            sb.AppendLine();
            sb.AppendLine("Amplitude Waveform (ASCII):");
            sb.AppendLine(new string('=', 80));

            var buffer = new float[audioSource.WaveFormat.SampleRate * audioSource.WaveFormat.Channels];
            int samplesRead;
            int samplesToShow = 200;
            int sampleSkip = Math.Max(1, (int)(audioSource.WaveFormat.SampleRate / samplesToShow));
            int sampleCount = 0;
            int lineCount = 0;
            const int maxLines = 50;

            while ((samplesRead = audioSource.Read(buffer, 0, buffer.Length)) > 0 && lineCount < maxLines)
            {
                for (int i = 0; i < samplesRead; i += audioSource.WaveFormat.Channels * sampleSkip)
                {
                    if (lineCount >= maxLines) break;
                    float amplitude = 0;
                    for (int ch = 0; ch < audioSource.WaveFormat.Channels && (i + ch) < samplesRead; ch++)
                        amplitude += Math.Abs(buffer[i + ch]);
                    amplitude /= audioSource.WaveFormat.Channels;
                    int barLength = (int)(amplitude * 40);
                    string bar = new string('█', Math.Min(barLength, 40));
                    string spaces = new string(' ', 40 - barLength);
                    sb.AppendLine($"{sampleCount,6}: [{bar}{spaces}] {amplitude:F4}");
                    sampleCount += sampleSkip;
                    lineCount++;
                    if (lineCount >= maxLines) break;
                }
            }
            return sb.ToString();
        }
        catch (Exception ex) { return $"Error visualizing waveform: {ex.Message}"; }
        finally { if (tempFile != null && File.Exists(tempFile)) try { File.Delete(tempFile); } catch { } }
    }

    private byte[] ConvertAudioFormat(byte[] audioBytes, string targetFormat)
    {
        string? tempFile = null;
        try
        {
            tempFile = System.IO.Path.GetTempFileName();
            File.WriteAllBytes(tempFile, audioBytes);
            var audioSource = LoadAudio(tempFile);

            if (targetFormat == "WAV")
            {
                var waveFormat = audioSource.WaveFormat;
                using var outputStream = new MemoryStream();
                using var writer = new WaveFileWriter(outputStream, waveFormat);
                var buffer = new float[waveFormat.SampleRate * waveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioSource.Read(buffer, 0, buffer.Length)) > 0)
                    writer.WriteSamples(buffer, 0, samplesRead);
                return outputStream.ToArray();
            }
            else if (targetFormat == "MP3")
            {
                using var outputStream = new MemoryStream();
                MediaFoundationEncoder.EncodeToMp3(audioSource.ToWaveProvider16(), outputStream, 128000);
                return outputStream.ToArray();
            }
            else throw new Exception($"Unsupported target format: {targetFormat}");
        }
        catch (Exception ex) { throw new Exception($"Error converting audio format: {ex.Message}"); }
        finally { if (tempFile != null && File.Exists(tempFile)) try { File.Delete(tempFile); } catch { } }
    }
}
