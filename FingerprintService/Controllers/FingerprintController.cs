using Microsoft.AspNetCore.Mvc;
using SourceAFIS;
using System;

[ApiController]
[Route("api/[controller]")]
public class FingerprintController : ControllerBase
{
    [HttpPost("match")]
    public IActionResult MatchFingerprints([FromBody] FingerprintData data)
    {
        // Decode the Base64 strings back into byte arrays
        byte[] probeImage = Convert.FromBase64String(data.Probe);
        byte[] candidateImage = Convert.FromBase64String(data.Candidate);

        // Convert the byte arrays to fingerprintImage
        FingerprintImage probeFingerprintImage = new FingerprintImage(probeImage);
        FingerprintImage candidateFingerprintImage  = new FingerprintImage(candidateImage);

        // Process images and create FingerprintTemplate objects
        FingerprintTemplate probeTemplate = new FingerprintTemplate(probeFingerprintImage);
        FingerprintTemplate candidateTemplate = new FingerprintTemplate(candidateFingerprintImage);

        var matcher = new FingerprintMatcher(probeTemplate);
        double score = matcher.Match(candidateTemplate);
        return Ok(new { Score = score, IsMatch = score >= 40 });
    }

    
}

public class FingerprintData
{
    public  string Probe { get; set; } = string.Empty;
    public  string Candidate { get; set; } = string.Empty;
}