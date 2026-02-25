#!/bin/sh
set -eu

CONFIG_TEMPLATE="/etc/frp/frps.toml.template"
CONFIG_FILE="/etc/frp/frps.toml"

FRPS_BIND_PORT="${FRPS_BIND_PORT:-7000}"
FRPS_VHOST_HTTP_PORT="${FRPS_VHOST_HTTP_PORT:-8080}"
FRPS_LOG_LEVEL="${FRPS_LOG_LEVEL:-info}"
FRPS_SUBDOMAIN_HOST="${FRPS_SUBDOMAIN_HOST:-}"

if [ -z "${FRP_TOKEN:-}" ]; then
  echo "FRP_TOKEN is required" >&2
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

echo "Starting frps on bindPort=${FRPS_BIND_PORT}, vhostHTTPPort=${FRPS_VHOST_HTTP_PORT}"
exec /usr/local/bin/frps -c "${CONFIG_FILE}"
