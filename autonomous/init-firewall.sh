#!/bin/bash
set -euo pipefail

ALLOWED_DOMAINS=(
    "github.com"
    "api.github.com"
    "uploads.github.com"
    "api.nuget.org"
    "globalcdn.nuget.org"
    "nuget.org"
    "api.anthropic.com"
    "sentry.io"
    "statsig.anthropic.com"
    "statsig.com"
    "registry.npmjs.org"
)

# ── Resolve everything BEFORE locking down ──
echo "Resolving domains..."

IPSET_NAME="allowed_hosts"
ipset create "$IPSET_NAME" hash:ip timeout 3600 2>/dev/null || ipset flush "$IPSET_NAME"

for domain in "${ALLOWED_DOMAINS[@]}"; do
    for ip in $(dig +short "$domain" 2>/dev/null | grep -E '^[0-9]'); do
        ipset add "$IPSET_NAME" "$ip" 2>/dev/null || true
    done
done

echo "Fetching GitHub CIDRs..."
GITHUB_CIDRS=$(curl -s --max-time 10 https://api.github.com/meta | \
    jq -r '.git[],.api[],.web[]' 2>/dev/null | head -40) || true

# ── NOW apply the firewall ──
echo "Applying rules..."

iptables -F OUTPUT
iptables -P OUTPUT DROP

# Loopback + established
iptables -A OUTPUT -o lo -j ACCEPT
iptables -A OUTPUT -m state --state ESTABLISHED,RELATED -j ACCEPT

# DNS
iptables -A OUTPUT -p udp --dport 53 -j ACCEPT
iptables -A OUTPUT -p tcp --dport 53 -j ACCEPT

# Allowed domains
iptables -A OUTPUT -m set --match-set "$IPSET_NAME" dst -j ACCEPT

# GitHub CIDRs
for cidr in $GITHUB_CIDRS; do
    iptables -A OUTPUT -d "$cidr" -j ACCEPT 2>/dev/null || true
done

echo "Firewall configured. Allowed: ${ALLOWED_DOMAINS[*]}"