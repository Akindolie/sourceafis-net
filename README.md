# 🔍 SourceAFIS .NET – REST API Extension + Docker

A fork of the open-source [SourceAFIS](https://github.com/robertvazan/sourceafis-net) fingerprint recognition engine, extended with a **RESTful Web API** and **Docker support** for seamless biometric fingerprint matching.

## 📦 Now with Docker Support!

You can now build and run the API in a containerized environment:

```bash
# Build Docker image
docker build -t sourceafis-api .

# Run container with fingerprint template folder mounted
docker run -d -p 5000:80 \
  -e FINGERPRINT_FOLDER_PATH=/app/fingerprints \
  -v /path/to/fingerprints:/app/fingerprints \
  --name afis-api \
  sourceafis-api
