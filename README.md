# 🔍 SourceAFIS .NET – REST API Extension

This is a fork and extension of the open-source [SourceAFIS](https://github.com/robertvazan/sourceafis-net) fingerprint recognition engine, redesigned to support **RESTful Web API access** using **ASP.NET Core**.

## 📌 Features

- 🧬 1-to-1 fingerprint matching endpoint (`/api/fingerprint/match-1-to-1`)
- 📂 1-to-1 lookup from stored templates by `EmployeeId` and `FingerCode` (`/api/fingerprint/match-1-to-1-lookup`)
- 🧠 1-to-N matching with parallel scoring to return the best match
- 🖼️ Base64 fingerprint decoding + image standardization using `ImageSharp`
- 🧾 Match score output with configurable threshold (default: `40`)
- ⚙️ Environment-driven file lookup path (`FINGERPRINT_FOLDER_PATH`)

## 🚀 Endpoints

### `POST /api/fingerprint/match-1-to-1`
Compares two base64-encoded fingerprint images.

#### Request
```json
{
  "probe": "base64-string",
  "candidate": "base64-string"
}
