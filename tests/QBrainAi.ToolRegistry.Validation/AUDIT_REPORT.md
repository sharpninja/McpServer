# Tool Registry Controller Endpoint Audit Report

**Date:** 2026-02-21  
**Service:** QBrain.AI on `http://localhost:7147`  
**Controller:** `ToolRegistryController` at `mcp/tools`  
**Auditor:** Cline / Claude Sonnet 4  
**Result:** ✅ **38/38 tests passed**

## Endpoints Audited

### Tool CRUD

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 1 | `GET` | `/qbrainai/tools` | Public | ✅ |
| 2 | `GET` | `/qbrainai/tools/search` | Public | ✅ |
| 3 | `GET` | `/qbrainai/tools/{id}` | Public | ✅ |
| 4 | `POST` | `/qbrainai/tools` | API Key | ✅ |
| 5 | `PUT` | `/qbrainai/tools/{id}` | API Key | ✅ |
| 6 | `DELETE` | `/qbrainai/tools/{id}` | API Key | ✅ |

### Bucket Management

| # | Method | Route | Auth | Status |
|---|--------|-------|------|--------|
| 7 | `GET` | `/qbrainai/tools/buckets` | Public | ✅ |
| 8 | `POST` | `/qbrainai/tools/buckets` | API Key | ✅ |
| 9 | `DELETE` | `/qbrainai/tools/buckets/{name}` | API Key | ✅ |
| 10 | `GET` | `/qbrainai/tools/buckets/{name}/browse` | Public | ✅ |
| 11 | `POST` | `/qbrainai/tools/buckets/{name}/install` | API Key | ✅ |
| 12 | `POST` | `/qbrainai/tools/buckets/{name}/sync` | API Key | ✅ |

## Key Findings

1. All 12 endpoints respond correctly with expected status codes and response schemas.
2. Read endpoints are public; write endpoints require API key authentication.
3. Tag-based search works correctly.
4. Bucket browse/sync return 404 gracefully when manifests don't exist at specified path.
5. Full tool CRUD lifecycle (create → get → update → search → delete) works end-to-end.
