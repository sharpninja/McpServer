workspace "Federation planning baseline" "Proposed deployment. DEPLOY HOLD remains in effect. Real hosts only; three-writer scenarios use an isolated fixture." {
    model {
        operator = person "Operator and plugins" "Explicit commits, branch work and reviewed reconciliation"
        federation = softwareSystem "QBrain.AI Federation" "Independent local work with Dolt committed history and separate audit delivery" {
            hApp = container "H local application and worker" "LAB working peer; gated live-branch writes; outbound sync and audit" ".NET / existing host"
            hDb = container "H project working database" "Independent drafts and versioned project history" "Dolt SQL / loopback 3306"
            hLocal = container "H operational store" "Local audit, leases, ID allocation and durable outboxes" "SQLite or supported provider"
            gateway = container "Federation gateway and publication API" "IdentityServer introspection; per-project remote relay; candidate validation; only publisher of public refs" "HTTPS / 10.42.0.214:7447"
            remote = container "Shared committed-history remote" "Separate from H working copy; backend reachable only by gateway" "Dolt remotesapi / loopback 7448"
            control = container "Hub control store" "Enrollment, subscriptions, receipts, aliases and restore epoch" "Unversioned relational database"
            audit = container "Hub append-only audit store" "Idempotent node audit records and durable receipts; never branchable" "Unversioned relational database"
            identity = container "Existing IdentityServer" "Per-node client credentials, owner binding, project scopes and revocation" "Duende / HTTPS gateway /connect"
            aApp = container "A local application and worker" "PAYTON-LEGION2; outbound federation only" ".NET / Windows service"
            aDb = container "A project working database" "Independent drafts and versioned project history" "Native Windows Dolt / loopback 3306"
            aLocal = container "A operational store" "Local audit, leases, IDs, aliases and durable outboxes" "SQLite or supported provider"
            backupsH = container "LAB backup set" "Project refs, controls, audit, IdentityServer and restore manifests" "Protected local backup files"
            backupsA = container "LEGION backup set" "Retained read-only SQLite sources, project refs and operational state" "F:/ protected backup files"
        }
        operator -> hApp "Local work and explicit commits"
        operator -> aApp "Local work and explicit commits"
        hApp -> hDb "Gated local reads/writes" "MySQL loopback"
        hApp -> hLocal "Audit/outbox/lease persistence"
        aApp -> aDb "Gated local reads/writes" "MySQL loopback"
        aApp -> aLocal "Audit/outbox/lease persistence"
        hApp -> gateway "Poll, upload candidate, accept request, fetch, audit push" "TLS / IdentityServer token"
        aApp -> gateway "Poll, upload candidate, accept request, fetch, audit push" "TLS / IdentityServer token"
        gateway -> identity "Token introspection and enrollment owner authorization" "Loopback / existing identity integration"
        gateway -> remote "Validated public-ref publication and guarded object relay" "Private remotesapi"
        gateway -> control "Generations, project authorization and receipts"
        gateway -> audit "Append deduplicated audit records and return receipts"
        hDb -> backupsH "Consistent checkpoint"
        hLocal -> backupsH "Consistent checkpoint"
        remote -> backupsH "Committed refs and objects"
        control -> backupsH "Checkpoint with restore epoch manifest"
        audit -> backupsH "Ledger and receipt checkpoint"
        identity -> backupsH "Protected identity recovery data"
        aDb -> backupsA "Consistent checkpoint"
        aLocal -> backupsA "Consistent checkpoint"
        deploymentEnvironment "Lab target" {
            deploymentNode "LAB-OMARCHY" "10.42.0.214; Linux; /home/sharpninja/github/McpServer" "Linux" {
                containerInstance hApp
                containerInstance hDb
                containerInstance hLocal
                containerInstance gateway
                containerInstance remote
                containerInstance control
                containerInstance audit
                containerInstance identity
                containerInstance backupsH
            }
            deploymentNode "PAYTON-LEGION2" "10.42.0.143; Windows; F:/github/McpServer; SSH kingd; no inbound federation listener" "Windows" {
                containerInstance aApp
                containerInstance aDb
                containerInstance aLocal
                containerInstance backupsA
            }
        }
    }
    views {
        container federation "Containers" {
            include *
            autoLayout lr
        }
        deployment federation "Lab target" "Deployment" {
            include *
            autoLayout lr
        }
        styles {
            element "Element" {
                color #173647
                background #eef5f7
                stroke #39747c
            }
            element "Container" {
                shape RoundedBox
            }
        }
    }
}
