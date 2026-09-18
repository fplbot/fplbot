// The OAuth `state` we mint is the page an install was started from, and it comes back through
// Slack/Discord, so treat it as untrusted until it is proven to be a path on this site.
export function isSiteRelativePath(value: unknown): value is string {
  return (
    typeof value === "string" &&
    value.startsWith("/") &&
    !value.startsWith("//") &&
    !value.startsWith("/\\") &&
    // eslint-disable-next-line no-control-regex
    !/[\u0000-\u001f\u007f]/.test(value)
  );
}
