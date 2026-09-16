---
name: hostile-review
description: Submit, status, get, and query MCP hostile review requests. Queue-and-record only. No repair or apply.
---

# Hostile Review

Use the shared hostile-review surfaces. Do not edit product files, TODOs, or requirements from review output.

```yaml
method: workflow.hostileReview.submit
method: workflow.hostileReview.status
method: workflow.hostileReview.get
method: workflow.hostileReview.query
```

Native MCP tools: `hostile_review_submit`, `hostile_review_status`, `hostile_review_get`, `hostile_review_query`.
Director: `hostile-review-submit`, `hostile-review-status`, `hostile-review-get`, `hostile-review-query`.
REST: `POST /mcpserver/hostile-review/submit`, `GET /mcpserver/hostile-review/{id}/status`, `GET /mcpserver/hostile-review/{id}`, `POST /mcpserver/hostile-review/query`.
