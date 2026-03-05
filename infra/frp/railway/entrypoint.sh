#!/bin/sh
set -eu

CONFIG_TEMPLATE="/etc/frp/frps.toml.template"
CONFIG_FILE="/etc/frp/frps.toml"

FRPS_BIND_PORT="${FRPS_BIND_PORT:-7000}"
FRPS_VHOST_HTTP_PORT="${FRPS_VHOST_HTTP_PORT:-8080}"
FRPS_LOG_LEVEL="${FRPS_LOG_LEVEL:-info}"
FRPS_SUBDOMAIN_HOST="${FRPS_SUBDOMAIN_HOST:-}"
FRPS_ALLOW_PORTS_START="${FRPS_ALLOW_PORTS_START:-}"
FRPS_ALLOW_PORTS_END="${FRPS_ALLOW_PORTS_END:-}"

if [ -z "${FRP_TOKEN:-}" ]; then
  echo "FRP_TOKEN is required" >&2
  exit 1
fi

if { [ -n "${FRPS_ALLOW_PORTS_START}" ] && [ -z "${FRPS_ALLOW_PORTS_END}" ]; } || \
   { [ -z "${FRPS_ALLOW_PORTS_START}" ] && [ -n "${FRPS_ALLOW_PORTS_END}" ]; }; then
  echo "FRPS_ALLOW_PORTS_START and FRPS_ALLOW_PORTS_END must be set together" >&2
  exit 1
fi

cp "${CONFIG_TEMPLATE}" "${CONFIG_FILE}"

sed -i \
  -e "s|__FRPS_BIND_PORT__|${FRPS_BIND_PORT}|g" \
  -e "s|__FRPS_VHOST_HTTP_PORT__|${FRPS_VHOST_HTTP_PORT}|g" \
  -e "s|__FRPS_LOG_LEVEL__|${FRPS_LOG_LEVEL}|g" \
  -e "s|__FRP_TOKEN__|${FRP_TOKEN}|g" \
  "${CONFIG_FILE}"

if [ -n "${FRPS_SUBDOMAIN_HOST}" ]; then
  printf '\nsubDomainHost = "%s"\n' "${FRPS_SUBDOMAIN_HOST}" >> "${CONFIG_FILE}"
fi

if [ -n "${FRPS_ALLOW_PORTS_START}" ] && [ -n "${FRPS_ALLOW_PORTS_END}" ]; then
  cat >> "${CONFIG_FILE}" <<EOF

allowPorts = [
  { start = ${FRPS_ALLOW_PORTS_START}, end = ${FRPS_ALLOW_PORTS_END} }
]
EOF
fi

echo "Starting frps on bindPort=${FRPS_BIND_PORT}, vhostHTTPPort=${FRPS_VHOST_HTTP_PORT}"
if [ -n "${FRPS_ALLOW_PORTS_START}" ] && [ -n "${FRPS_ALLOW_PORTS_END}" ]; then
  echo "Restricting FRP TCP remote ports to ${FRPS_ALLOW_PORTS_START}-${FRPS_ALLOW_PORTS_END}"
fi
exec /usr/local/bin/frps -c "${CONFIG_FILE}"
