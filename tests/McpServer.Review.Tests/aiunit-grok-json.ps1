param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $PromptParts
)

$ErrorActionPreference = 'Stop'
$prompt = ($PromptParts -join ' ')
$schema = @'
{
  "type": "object",
  "additionalProperties": false,
  "required": ["schemaVersion", "reviewType", "status", "summary", "findings"],
  "properties": {
    "schemaVersion": { "const": "aiunit.review.findings.v1" },
    "reviewType": { "const": "project" },
    "status": { "enum": ["pass", "fail", "error"] },
    "summary": { "type": "string" },
    "reviewedScope": { "type": "string" },
    "agent": {
      "type": "object",
      "additionalProperties": false,
      "required": ["name"],
      "properties": {
        "name": { "type": "string" },
        "provider": { "type": "string" },
        "model": { "type": "string" }
      }
    },
    "findings": {
      "type": "array",
      "items": {
        "type": "object",
        "additionalProperties": false,
        "required": ["severity", "title", "detail", "recommendation"],
        "properties": {
          "severity": { "enum": ["critical", "high", "medium", "low", "info"] },
          "category": { "type": "string" },
          "title": { "type": "string" },
          "detail": { "type": "string" },
          "recommendation": { "type": "string" },
          "filePath": { "type": "string" },
          "line": { "type": "integer", "minimum": 1 },
          "ruleId": { "type": "string" },
          "confidence": { "type": "number", "minimum": 0, "maximum": 1 },
          "agent": { "type": "string" }
        }
      }
    }
  }
}
'@

$response = grok --single $prompt --json-schema $schema
$envelope = $response | ConvertFrom-Json
if ($null -ne $envelope.structuredOutput) {
    $envelope.structuredOutput | ConvertTo-Json -Depth 20 -Compress
    exit 0
}

if ($null -ne $envelope.text) {
    $envelope.text
    exit 0
}

$response
