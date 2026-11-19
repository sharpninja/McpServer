# Task003 native Dolt client integration: source-backed decision

Author: Codex. Date: 2026-11-19. Status: operator-selected narrow Dolt client patch for disposable Phase0 proof. No patch implementation or installation yet. Task TASK-FEDPH0-003 remains Planning while selected-route design/mock closure is completed. All-nine implementation barrier remains Closed.

Research provider: official GitHub primary-source fallback. No Perplexity MCP or native pplx application is exposed in this session. The discovered pplx name is a SnippetsAliasManager function, without a verified authenticated research-CLI route.

## Approved outcome and decision boundary

The approved [architecture section 13](2026-11-10-federation-dolt-architecture.md#13-existing-identityserver-is-the-authority) requires protected node reference-token relay, per-request gateway introspection, project isolation and gateway-only backend credentials. It directs the owner to stop that gate and present an alternative if supported client/proxy integration cannot enforce these outcomes. The engine selection is Dolt; permission to patch an EF provider does not by itself select a new Dolt distribution.

Trace: FR-MCP-185, TR-MCP-FEDERATION-012 and TEST-MCP-233, especially ac-RawRemoteCannotBypassPublicationPolicy, ac-ForeignProjectCloneDenied and ac-RevokedCredentialFailsFetchAndPublish. TLS and credential-containment work also carries ac-UnknownCertificatePinRejected and FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials through Task007. No criterion is added or marked satisfied here.

Operator decision2026-11-19: "Use the narrow Dolt client patch (Recommended)." This selects a narrowly patched build of the already-selected Dolt client for disposable Ph0 proof. Implementation of this route is authorized when task dependencies and the all-nine design/mock barrier permit it. The decision does not open that barrier, authorize live deployment, accept B5 or establish native feasibility.

## Correction to the preceding design

The live canonical records and current contracts chapter require a protected reference token but do not prescribe the native relay header name. The prior standard-Authorization-only framing was an agent-added constraint and is withdrawn.

The pinned upstream helper intentionally preserves ordinary Dolt Authorization while adding Proxy-Authorization. Its tests explicitly demonstrate a bearer value on unary gRPC, streaming metadata and object HTTP. This is an existing upstream credential channel, not evidence that the gateway already accepts it or that end-to-end revocation works. The assertion seam records independently verified credential classes without assuming a header name.

The stock helper's rejection of Authorization is therefore not sufficient reason to fork Dolt. The selected narrow patch is justified by the separate origin/TLS/PowerShell-launch/diagnostic gaps below. Do not replace reference tokens with Basic passwords or signing-key JWTs merely because those stock gRPC mechanisms exist.

## Verified source findings

All Dolt findings use candidate source commit 9e4102e303610fc4681de16a9f7bbbc870d884a3. Full source text, canonical records, SHA256 readback and provenance are retained in [the decision evidence](../receipts/federation/20261119-task003-native-client-options/source-research.json).

- [credential_helper_test.go](https://github.com/dolthub/dolt/blob/9e4102e303610fc4681de16a9f7bbbc870d884a3/go/libraries/doltcore/credentialhelper/credential_helper_test.go): the unary, stream and HTTP tests exercise Proxy-Authorization bearer; the separate Authorization header is preserved. The executable-protocol test uses a POSIX shell and skips Windows. Reading that test is not a test run.
- [credential_helper.go](https://github.com/dolthub/dolt/blob/9e4102e303610fc4681de16a9f7bbbc870d884a3/go/libraries/doltcore/credentialhelper/credential_helper.go): helper results use the existing URI-in, headers/expiry-out JSON protocol. The command is one executable followed by get. Nonmatching HTTP origins are delegated rather than refused. Redirects are delegated to the underlying client. Helper stderr is included in an error on command failure.
- [grpc_dial_provider.go](https://github.com/dolthub/dolt/blob/9e4102e303610fc4681de16a9f7bbbc870d884a3/go/libraries/doltcore/env/grpc_dial_provider.go): helper interceptors and HTTP wrapping cover separate gRPC and HTTP paths. The default HTTP client has no configured CheckRedirect function. A non-nil TLSConfig can be supplied through the library configuration.
- [grpcendpoint/config.go](https://github.com/dolthub/dolt/blob/9e4102e303610fc4681de16a9f7bbbc870d884a3/go/libraries/doltcore/grpcendpoint/config.go) and [dbfactory/grpc.go](https://github.com/dolthub/dolt/blob/9e4102e303610fc4681de16a9f7bbbc870d884a3/go/libraries/doltcore/dbfactory/grpc.go): GRPCDialProvider returns independent DialOptions and HTTPFetcher; the factory passes both into Dolt's existing remote chunk-store implementation.
- [creds.go](https://github.com/dolthub/dolt/blob/9e4102e303610fc4681de16a9f7bbbc870d884a3/go/libraries/doltcore/creds/creds.go): built-in password credentials generate Basic metadata, while Dolt key credentials sign a Dolt-specific JWT. Neither observation establishes the required reference-token relay across object HTTP.
- [Dolt go.mod](https://github.com/dolthub/dolt/blob/9e4102e303610fc4681de16a9f7bbbc870d884a3/go/go.mod) declares Go 1.27.3. This is source metadata, not a measured build identity for an installed Dolt binary.
- [Go 1.27.3 HTTP client](https://github.com/golang/go/blob/go1.27.3/src/net/http/client.go), particularly makeHeadersCopier, shouldCopyHeaderOnRedirect and defaultCheckRedirect: Proxy-Authorization is sensitive, but the default copying rule permits matching hosts and subdomains without an exact scheme/port comparison. The default redirect callback only limits the number of redirects. A foreign-host redirect can still be followed with credentials removed; that does not meet the existing exact-origin refusal assertion.

Dolt source headers declare Apache-2.0. The retained [Go license](https://github.com/golang/go/blob/go1.27.3/LICENSE) is BSD-3-Clause. Actual distribution licensing, toolchain/build hashes and notices remain B1 evidence obligations for the selected patched-client route.

## Selected route: narrow patch to the selected native client

Preserve Dolt's remotesapi, object hashing, transfer, history, commit, fetch and push algorithms. Preserve the existing helper JSON exchange and its supported Proxy-Authorization channel. Add no custom merge engine, replication algorithm, database engine, alternate identity authority, or reference-token revocation ledger.

The selected patch has four bounded surfaces. The exact opt-in policy, launch and TLS observations are specified in [the selected native-client contract](2026-11-19-federation-native-client-contract.md):

1. In the existing helper/dial-provider HTTP path, add a strict credential-origin mode for the isolated federation client. Validate every advertised object URL before any outbound connection and every redirect before the next outbound request. Require the configured HTTPS scheme, canonical host and effective port, no user info and no fragment. Refuse foreign host, subdomain, changed port, downgrade and user-info targets locally. Preserve the original sealed path/query bytes on permitted requests. Do not restrict all ordinary Dolt remotes globally.
2. Use a single explicit TLS policy for the protected gRPC and object HTTP channels: approved CA trust, expected SAN/hostname and approved SPKI pins. A mismatch fails before any bearer request. The source exposes a library TLS configuration seam; the selected native CLI path must expose or wire the policy, not assume the library field already makes it configurable. Task007 owns the complete TLS/canary acceptance evidence.
3. Add a structured helper argument-vector option so native Windows and Linux Dolt can launch pwsh with -NoProfile -NonInteractive -File and an absolute helper script path, then append the existing get operation. Parse an argument array, never a shell command string. No credential is an argument. Keep the executable-only helper form compatible. The helper remains PowerShell; no Bash script, C# credential-tool executable or Python/Node helper is introduced.
4. Keep helper failure diagnostics safe. Do not embed helper stderr, response values, bearer values or credential-bearing URI input in public errors. Report a stable safe error and exit condition; retain no raw credential dump. Secret-canary negative controls must prove the error path is safe.

These are proposed source changes to Dolt's existing product client, not implementation performed by this document. Their exact source diff, build recipe, notices and platform packages must be retained before use. Implementation and disposable use follow the operator selection and all-nine readiness/dependency gates in this document; live deployment remains outside this proof.

## Credential flow and ownership

The disposable native harness prepares a node-owned protected token source. The PowerShell helper accepts the existing URI JSON on stdin, validates the allowed gateway origin, obtains the invocation-local reference token through a protected source, and returns the existing helper response on stdout. The helper's stdout is consumed in memory by Dolt and never copied to a transcript or receipt.

For Ph0, use the integration probe's private in-memory token broker with a current-user-only named pipe. A non-secret pipe reference may be supplied in process-scoped environment; the token is not supplied in argv or a generated file. The broker validates the request's origin and fixture identity, uses the existing URI/header response shape and returns no token on mismatch. PowerShell/.NET named-pipe ownership and permissions must be proved on each platform before credentials are used. This is a proposed disposable fixture boundary, not a new public credential API or maintained secret store.

The public remote gateway consumes the native Proxy-Authorization bearer channel for this selected remote route, checks the reference token through the existing issuer on every new request, resolves project membership before existence disclosure and strips all incoming credential/forwarding headers before adding gateway-owned backend credentials over loopback. Ordinary control/publication APIs retain their own supported authentication surface. Ambiguous duplicate/mixed native credential inputs fail closed rather than selecting an attacker-controlled value.

Helper token caching until expiry is distinct from gateway introspection caching. A helper may reuse an opaque token while the gateway still introspects every new request; after revocation the same token must fail. An already-open streaming call still validates every message's repository selector before foreign transfer.

No node token or ordinary local application identity gains backend remote administrative authority. Public-ref advancement remains exclusively publication.accept and its fenced publisher, with introspection repeated at the commit boundary.

## Alternatives considered

- Stock helper plus stock HTTP defaults: retain as the baseline control, not an accepted result. It exposes the intended bearer channel but source-level redirect behavior does not meet the current containment assertions. A source read alone cannot prove the complete gateway/client pairing.
- Standard Authorization helper replacement: rejected as the rationale for a fork. The approved requirement does not mandate that native header, and upstream already supplies a separate proxy credential channel.
- A new embedded Go client host around GRPCDialProvider and HTTPFetcher: a possible operator-selected alternative. It can reuse Dolt's transfer implementation, but adds a new first-party native host and lifecycle/packaging surface. It is not adopted here and must not be disguised as a PowerShell automation script.
- A raw remotesapi listener or alternate public header that bypasses the gateway: rejected by the approved isolation and publication requirements.
- Wait for an upstream-supported strict client route: valid if the operator declines maintaining the narrow patch. Task003 then stays Planning; other permitted design/mock tasks may continue.

## Ordered work after route selection

1. Record the operator's selection in Task003 and the session log without changing its acceptance criteria.
2. Complete the selected route's source-level design, PowerShell launch contract, gateway repository/method binding and current-definition assertion mapping. Review these against all current Task003 publication/recovery and authorization cases.
3. Add independent discriminating mocks for any newly specified fixture observation before its implementation. Establish current-definition green mock evidence and readiness for every implementation member. Do not start the client patch, native helper, token broker, gateway or real adapter before the all-nine barrier opens.
4. Once permitted by task dependencies and the barrier, implement the disposable real bindings. Retain exact upstream base, patch, compiler/driver package versions, licenses, build commands and Windows/Linux binary hashes.
5. Run the SAME contract assertions on both hosts against actual native Dolt and the exact existing issuer. Capture independent node/gateway/backend observations, repository refs, object-transfer counts and secret-safe failures. Exercise all 12 RPCs, streaming selectors, range/full downloads, upload, URL refresh, retry and all redirect cases.
6. Prove real TLS rejection before transmission, revocation before the next transfer/publication, identity outage, raw public-ref refusal, foreign-project denial, helper launch on Windows/Linux and process-death publication recovery. The upstream skipped Windows test cannot satisfy this obligation.
7. Produce separate LAB and LEGION B1/B5/B12 evidence rows. Follow the complete Task Group exit gate, operator B acceptance, final-head full gates and independent HV. This document authorizes no task Success or group completion.

## Current disposition

No patched client, new native host, credential helper, broker or gateway has been implemented or installed. No native test or two-host feasibility claim is made. Existing transport mocks remain historical assertion evidence only.

Task003 remains Planning with the narrow-patch route selected and selected-route design/readiness pending. Other members' readiness, all-nine barrier, dependencies and DEPLOY HOLD are unchanged. The full Task Group1 objective remains active and incomplete.
