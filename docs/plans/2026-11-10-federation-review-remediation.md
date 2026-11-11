# Federation review remediation

Author: Codex. Date:2026-11-10. Planning changes only; no new independent HV verdict or implementation acceptance. DEPLOY HOLD remains active. The withdrawn LocalProxy, scope and manual-commit objections stay withdrawn.

1. **Audit reaches the hub:** amend FR-MCP-AUDIT-001, TR-MCP-AUDIT-004, TEST-MCP-AUDIT-003 and FR-MCP-184. Permanent local audit pushes to an append-only unversioned hub ledger independently of commits, with origin IDs, hashes and receipts. TODO/generic audit backfill is verified. Split the old exclusion criterion into CloneOmitsSecrets, AuditReachesHub and AuditBackfillVerified. Ph2/Ph3/Ph5.

2. **Legacy authority:** amend FR-MCP-103/TR-MCP-FED-001/TEST-MCP-136 to autonomous versioned federation. Retain explicitly selected DirectProxy under FR-MCP-077. Every original AC has a disposition in the reconciliation register.

3. **IdentityServer:** reuse the existing Duende integration and MCP-FEDIDENTITY-001. Define single-user owner binding, per-node clients, project scopes, reference-token introspection and IdentityServer revocation. Gateway authenticates remote traffic; native Dolt bearer validation is not assumed. Ph0 proves the protocol/credential relay and object/redirect containment. Ph2/Ph5/Ph7 implement the accepted contract.

4. **Structurizr:** workspace.dsl defines container and real-host deployment views, locally validated/exported with the cached CLI. Workers reach the guarded gateway, never directly publish public refs. Include remote/SQL distinction, both operational stores, identity, audit and backups. PDF embeds generated vector views.

5. **Staging:** real acceptance uses LAB and LEGION. Nuke's named disposable FED-ATLAS-H/A/B fixture supplies three-writer scenarios; Ph0 proves its provisioning. No third physical machine is claimed or provisioned during planning.

6. **Concrete TR/TEST:** replace boilerplate with 16 technical contracts and 91 unique GWT scenarios containing schemas, routes, scopes, captured fixture hashes, row values and error codes. Original AC text is preserved; all amended criteria remain pending.

7. **Join sequence:** Ph3 delivers seed/planner/resume/verify and TODO recovery export. Join is unavailable until Ph6, after Ph4 commit/branch and Ph5 publication services exist. Ph6 owns reviewed join/main verification.

8. **IDs:** detect similar work under different IDs, require review and retain permanent origin-scoped aliases. Historical text is never rewritten. Atomic human-ID reservations produce one winner; offline work retains tagged provisional IDs. Ph2/Ph3/Ph5/Ph6.

9. **Review visibility:** safe TODO rules permit identical or independent valid cells, never timestamp winners for conflicts. Status/plugins/operator inbox warn after15minutes and escalate after24hours. Unrelated branches/projects publish while one branch waits. No implicit external messaging. Ph6/Ph7.

10. **Windows:** select native checksum-pinned dolt.exe supervised by LEGION's existing Windows service, F:/ storage and handle/mutex ownership checks. Linux uses the pinned official container. Every B1-B12 receipt has LAB and LEGION evidence; lifecycle is a planned contract, not a deployment claim.

11. **Recovery:** Ph3 exports a frozen Dolt head's TODO/child/link/alias graph to a new verified SQLite recovery DB. Original SQLite sources remain read-only. This is not full-database reverse migration; non-TODO changes need an explicit recovery disposition. Ph8 includes one-node 48-hour soak before the second host.

12. **Network:** LAB10.42.0.214 and LEGION10.42.0.143 on10.42.0.0/24. SQL3306 is loopback-only, gateway7447 binds LAB's link address, backend remotesapi7448 stays loopback-only; no new inbound federation listener on LEGION. Proposed issuer QBrain Lab Federation Root CA is not claimed to exist. Ph0 records actual root/SPKI fingerprints, SANs and rotation ownership; no TOFU or TLS bypass. Test cross-link3306 denial.

13. **Secrets:** credentials never appear in argv, receipts, logs, chat, memory or commits. Protected references/files inject into process-private environment; receipt commands contain only nonsecret arguments. Canary scans run on both OSs.

14. **HV scope:** cover says planning baseline only and revised after review. Historical 98/98 covers only the old six-file packet, not this PDF/revision. Reviewer effort was not independently verified. R4-L1 ownership and R4-L2 live-branch wording are corrected. Future HV uses an agent family other than the author and the complete reconciliation packet.

15. **DEPLOY HOLD:** Ph8 requires explicit operator release for the exact rollout. SSH to LEGION remains kingd with established authorized configuration. No deployment, migration or activation occurred here.

16. **Walkthroughs:** U06 shows H's local connectivity, unpublished A1/B1 and M1 then M2. U11 merges retained B1 with S1 into M1. U10 shows SQLite on both real hosts and a labeled test peer. U13-U15 add audit, aliases and restore; total15 workflows/58 states.

17. **Restore:** create a fresh non-reused HubEpoch before reopening ingress; reject old-epoch reports, preserve node heads, validate missing-object recovery and reconcile audit receipt inventories. HubRestoreFromOlderBackup belongs to Ph5/Ph6/Ph8.

Retained strengths: no force pushes, requestId trailers, generation/worker fencing, separate hub working and shared remote stores, no business-command replay, one migration root and source retention.

The full register additionally reconciles old memory forwarding, path-rekey D1/D5/D6/D7, hub-owned scope-layer D3, no-fanout D2, superseded read-switch D4, full session graphs and the later QuadBrain-only keyserver decision. These are explicit plan inputs, not rediscovered reviewer surprises.
