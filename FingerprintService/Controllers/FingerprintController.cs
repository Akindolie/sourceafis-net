using Microsoft.AspNetCore.Mvc;
using SourceAFIS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using DotNetEnv;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

[ApiController]
[Route("api/[controller]")]
public class FingerprintController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, List<FingerprintTemplateWithFileName>> templateCache = new();
    private readonly double matchThreshold = 40; // Set your match threshold
    private readonly object locker = new();

    public FingerprintController()
    {
        DotNetEnv.Env.Load();
    }

    [HttpPost("match-1-to-1")]
    public IActionResult MatchFingerprints1To1([FromBody] FingerprintMatch1To1Data data)
    {
        try
        {
            // Decode the Base64 strings back into byte arrays
            byte[] probeImage = Convert.FromBase64String(data.Probe);
            byte[] candidateImage = Convert.FromBase64String(data.Candidate);

            // Convert the byte arrays to fingerprintImage
            FingerprintImage probeFingerprintImage = new FingerprintImage(probeImage);
            FingerprintImage candidateFingerprintImage = new FingerprintImage(candidateImage);

            // Process images and create FingerprintTemplate objects
            FingerprintTemplate probeTemplate = new FingerprintTemplate(probeFingerprintImage);
            FingerprintTemplate candidateTemplate = new FingerprintTemplate(candidateFingerprintImage);

            var matcher = new FingerprintMatcher(probeTemplate);
            double score = matcher.Match(candidateTemplate);
            return Ok(new { Score = score, IsMatch = score >= matchThreshold });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("match-1-to-1-lookup")]
    public IActionResult MatchFingerprints1To1Lookup([FromBody] FingerprintMatch1To1LookupData data)
    {
        try
        {
            string folderPath = Environment.GetEnvironmentVariable("FINGERPRINT_FOLDER_PATH")
                                ?? throw new Exception("FINGERPRINT_FOLDER_PATH is not set.");

            byte[] probeImage = Convert.FromBase64String(data.Probe);
            FingerprintImage probeFingerprintImage = new FingerprintImage(probeImage);
            FingerprintTemplate probeTemplate = new FingerprintTemplate(probeFingerprintImage);

            string candidateFile = Path.Combine(folderPath, $"{data.EmployeeId}_{data.FingerCode}.txt");
            if (!System.IO.File.Exists(candidateFile))
                return NotFound(new { Error = $"Candidate fingerprint for EmployeeId '{data.EmployeeId}' and FingerCode '{data.FingerCode}' not found." });

            string base64Image = System.IO.File.ReadAllText(candidateFile).Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
            byte[] imageBytes = Convert.FromBase64String(base64Image);

            using var ms = new MemoryStream(imageBytes);
            using var decoded = Image.Load<Argb32>(ms);
            using var outputStream = new MemoryStream();
            decoded.SaveAsPng(outputStream);
            byte[] validatedBytes = outputStream.ToArray();

            FingerprintTemplate candidateTemplate = new FingerprintTemplate(new FingerprintImage(validatedBytes));

            var matcher = new FingerprintMatcher(probeTemplate);
            double score = matcher.Match(candidateTemplate);

            return Ok(new { Score = score, IsMatch = score >= matchThreshold, FileName = Path.GetFileName(candidateFile) });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("match-1-to-n")]
    public IActionResult MatchFingerprints1ToN([FromBody] FingerprintMatch1ToNData data)
    {
        try
        {
            byte[] probeImage = Convert.FromBase64String(data.Probe);
            FingerprintTemplate probeTemplate = new FingerprintTemplate(new FingerprintImage(probeImage));

            var candidateTemplates = LoadTemplatesByFingerCode(data.FingerCode);
            // if (candidateTemplates.Count == 0)
            //     return NotFound(new { Error = $"No fingerprint templates found for FingerCode '{data.FingerCode}'." });

            double highestScore = 0;
            FingerprintTemplateWithFileName? bestMatch = null;

            // Use parallel processing to speed up matching
            Parallel.ForEach(candidateTemplates, () => (score: 0.0, match: (FingerprintTemplateWithFileName?)null),
                (candidate, state, local) =>
                {
                    var matcher = new FingerprintMatcher(probeTemplate);
                    double score = matcher.Match(candidate.Template);
                    if (score > local.score)
                    {
                        local = (score, candidate);
                    }
                    return local;
                },
                local =>
                {
                    lock (locker)
                    {
                        if (local.score > highestScore)
                        {
                            highestScore = local.score;
                            bestMatch = local.match;
                        }
                    }
                });

            if (highestScore >= matchThreshold && bestMatch != null)
            {
                return Ok(new { Score = highestScore, IsMatch = true, FileName = bestMatch.FileName });
            }
            else
            {
                return Ok(new { Score = highestScore, IsMatch = false });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    private List<FingerprintTemplateWithFileName> LoadTemplatesByFingerCode(string fingerCode)
    {
        // if (templateCache.ContainsKey(fingerCode))
        //     return templateCache[fingerCode];

        string folderPath = Environment.GetEnvironmentVariable("FINGERPRINT_FOLDER_PATH")
                            ?? throw new Exception("FINGERPRINT_FOLDER_PATH is not set.");

        List<FingerprintTemplateWithFileName> list = new();

        foreach (var filePath in Directory.GetFiles(folderPath, $"*_{fingerCode}.txt"))
        {
            try
            {
                string base64Image = System.IO.File.ReadAllText(filePath).Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
                byte[] imageBytes = Convert.FromBase64String(base64Image);

                using var ms = new MemoryStream(imageBytes);
                using var decoded = Image.Load<Argb32>(ms);
                using var outputStream = new MemoryStream();
                decoded.SaveAsPng(outputStream);
                byte[] validatedBytes = outputStream.ToArray();

                var template = new FingerprintTemplate(new FingerprintImage(validatedBytes));

                list.Add(new FingerprintTemplateWithFileName
                {
                    Template = template,
                    FileName = Path.GetFileName(filePath)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load {filePath}: {ex.Message}");
            }
        }

        //templateCache[fingerCode] = list;
        return list;
    }

    public byte[] ConvertToArgbPng(byte[] originalImage)
    {
        using (var image = Image.Load(originalImage))
        {
            image.Mutate(x => x.BackgroundColor(Color.White));
            using (var ms = new MemoryStream())
            {
                image.SaveAsPng(ms, new PngEncoder
                {
                    ColorType = PngColorType.RgbWithAlpha
                });
                return ms.ToArray();
            }
        }
    }
}

public class FingerprintTemplateWithFileName
{
    public FingerprintTemplate? Template { get; set; }
    public string FileName { get; set; } = string.Empty;
}

public class FingerprintMatch1To1Data
{
    public string Probe { get; set; } = string.Empty;
    public string Candidate { get; set; } = string.Empty;
}

public class FingerprintMatch1To1LookupData
{
    public string Probe { get; set; } = string.Empty;
    public string FingerCode { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
}

public class FingerprintMatch1ToNData
{
    public string Probe { get; set; } = string.Empty;
    public string FingerCode { get; set; } = string.Empty;
}
