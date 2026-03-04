# =============================================================================
# HdrHistogram.NET Agent Container
# .NET SDK + Claude Code + gh CLI + firewall
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim
RUN dotnet_version=8.0 \
    && curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
       --runtime dotnet --channel $dotnet_version --install-dir /usr/share/dotnet \
    && dotnet_version=9.0 \
    && curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
       --runtime dotnet --channel $dotnet_version --install-dir /usr/share/dotnet

ARG CLAUDE_CODE_VERSION="latest"

# ---------- System deps + firewall tooling ----------
RUN apt-get update && apt-get install -y --no-install-recommends \
    git curl wget jq ca-certificates gnupg openssh-client \
    iptables ipset dnsutils iproute2 \
    sudo \
    && rm -rf /var/lib/apt/lists/*

# ---------- Node.js (required for Claude Code) ----------
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# ---------- GitHub CLI ----------
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
      | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
      | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt-get update && apt-get install -y gh \
    && rm -rf /var/lib/apt/lists/*

# ---------- Claude Code ----------
ENV NPM_CONFIG_PREFIX=/usr/local/share/npm-global
ENV PATH=$NPM_CONFIG_PREFIX/bin:$PATH
RUN npm install -g @anthropic-ai/claude-code@${CLAUDE_CODE_VERSION}

# ---------- Agent user ----------
RUN useradd -m -s /bin/bash agent \
    && mkdir -p /home/agent/.claude /workspace \
    && chown -R agent:agent /home/agent/.claude /workspace

# ---------- Firewall ----------
COPY init-firewall.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/init-firewall.sh \
    && echo "agent ALL=(root) NOPASSWD: /usr/local/bin/init-firewall.sh" \
       > /etc/sudoers.d/agent-firewall \
    && chmod 0440 /etc/sudoers.d/agent-firewall

# ---------- Entrypoint ----------
COPY entrypoint.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/entrypoint.sh

# ---------- Agent loop ----------
COPY agent-loop.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/agent-loop.sh

# ---------- Give access to the nuget cache vol ----------
RUN mkdir -p /home/agent/.nuget/NuGet /home/agent/.nuget/packages \
    && chown -R agent:agent /home/agent/.nuget

COPY prompts/ /usr/local/share/agent-prompts/

USER agent
WORKDIR /workspace

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]