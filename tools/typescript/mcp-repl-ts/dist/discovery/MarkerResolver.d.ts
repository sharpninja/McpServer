export interface MarkerContext {
    markerFile: string;
    baseUrl: string;
    apiKey: string;
    workspace: string;
    workspacePath: string;
    port: string;
}
/**
 * Walk up from startDir looking for AGENTS-README-FIRST.yaml.
 * Returns the full path or null if not found.
 * Mirrors find_marker_file() in lib/marker-resolver.sh.
 */
export declare function findMarkerFile(startDir: string): string | null;
/**
 * Extract a top-level YAML field using line-by-line parsing (no yq dependency).
 * Also handles nested fields under endpoints:.
 * Mirrors parse_marker_field() in lib/marker-resolver.sh.
 */
export declare function parseMarkerField(markerFile: string, fieldName: string): string | null;
/**
 * Verify the HMAC-SHA256 signature in the marker file.
 * Mirrors verify_signature() in lib/marker-resolver.sh.
 */
export declare function verifySignature(markerFile: string): boolean;
/**
 * Orchestrate: find marker -> parse -> verify -> health nonce check.
 * Returns MarkerContext or throws.
 * Mirrors full_bootstrap() in lib/marker-resolver.sh.
 */
export declare function fullBootstrap(startDir?: string): Promise<MarkerContext>;
