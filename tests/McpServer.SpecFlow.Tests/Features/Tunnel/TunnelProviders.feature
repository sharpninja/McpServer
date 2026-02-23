Feature: Tunnel Providers
  As a server operator
  I want the server to expose itself to the internet via pluggable tunnel providers
  So that remote AI agents can connect to a locally running server

  # FR-MCP-015 / TR-MCP-TUN-001 / TR-MCP-TUN-002 / TR-MCP-TUN-003 / TEST-MCP-011

  Scenario: Ngrok tunnel provider uses NGROK_AUTHTOKEN environment variable
    # TR-MCP-TUN-003 — auth token passed via env var, not CLI argument
    Given the NgrokTunnelProvider is configured with auth token "test-token"
    When the NgrokTunnelProvider builds its process start info
    Then the process arguments should not contain the auth token
    And the environment variable "NGROK_AUTHTOKEN" should be set to "test-token"

  Scenario: Tunnel process stop is TOCTOU-safe via try-catch on Process.Kill
    # TR-MCP-TUN-002 — Process.Kill wrapped in try-catch for InvalidOperationException
    Given a tunnel process has already exited before StopAsync is called
    When StopAsync is called on the tunnel provider
    Then no InvalidOperationException is thrown

  Scenario: Tunnel process stop waits up to 5 seconds for graceful exit
    # TR-MCP-TUN-002 — WaitForExit(5000) timeout
    Given a tunnel provider has an active process
    When StopAsync is called
    Then the provider waits at most 5000 milliseconds for the process to exit

  Scenario: FRP tunnel config file is deleted after stop
    # TR-MCP-TUN-002 — FRP config files cleaned up
    Given the FrpTunnelProvider wrote a config file
    When StopAsync is called on the FrpTunnelProvider
    Then the FRP config file should be deleted

  Scenario: Tunnel provider is registered as IHostedService when provider name is set
    # TR-MCP-TUN-001 — registered as IHostedService conditionally
    Given tunnel provider "ngrok" is configured
    Then the DI container should contain a registered IHostedService for the tunnel provider

  Scenario: Cloudflare tunnel uses cloudflared quick tunnel when no named tunnel configured
    Given the CloudflareTunnelProvider is configured without a tunnel name
    When the CloudflareTunnelProvider builds its process start info
    Then the process arguments should contain "tunnel"
    And the process arguments should contain "run"
