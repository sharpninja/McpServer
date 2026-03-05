FROM alpine:3.20

ARG FRP_VERSION=0.61.1
ARG TARGETARCH=amd64

RUN apk add --no-cache ca-certificates curl tar

RUN set -eu; \
    case "${TARGETARCH}" in \
      amd64) FRP_ARCH="amd64" ;; \
      arm64) FRP_ARCH="arm64" ;; \
      *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac; \
    FRP_FILE="frp_${FRP_VERSION}_linux_${FRP_ARCH}.tar.gz"; \
    FRP_URL="https://github.com/fatedier/frp/releases/download/v${FRP_VERSION}/${FRP_FILE}"; \
    curl -fsSL "${FRP_URL}" -o /tmp/frp.tar.gz; \
    tar -xzf /tmp/frp.tar.gz -C /tmp; \
    mv "/tmp/frp_${FRP_VERSION}_linux_${FRP_ARCH}/frps" /usr/local/bin/frps; \
    chmod +x /usr/local/bin/frps; \
    rm -rf /tmp/frp.tar.gz "/tmp/frp_${FRP_VERSION}_linux_${FRP_ARCH}"

RUN mkdir -p /etc/frp

COPY entrypoint.sh /usr/local/bin/entrypoint.sh
COPY frps.toml.template /etc/frp/frps.toml.template

RUN chmod +x /usr/local/bin/entrypoint.sh

ENV FRPS_BIND_PORT=7000 \
    FRPS_VHOST_HTTP_PORT=8080 \
    FRPS_LOG_LEVEL=info

EXPOSE 7000 8080

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
