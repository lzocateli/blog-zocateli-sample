FROM lzocateli/dotnet-sdk:10.0.400-noble

ENV HTTP_PROXY=
ENV HTTPS_PROXY=
ENV NO_PROXY=

RUN apt update -y \
    && apt upgrade -y \
    && apt install -y zsh zsh-autosuggestions

ARG TZ=America/Sao_Paulo
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ >/etc/timezone

# --- Node.js 22 LTS via NodeSource ---
RUN curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt install -y nodejs unzip \
    && npm install -g @angular/cli

# --- Bun ---
RUN curl -fsSL https://bun.sh/install | bash
ENV PATH="/root/.bun/bin:$PATH"

# --- Deno ---
RUN curl -fsSL https://deno.land/install.sh | sh
ENV DENO_INSTALL="/root/.deno"
ENV PATH="$DENO_INSTALL/bin:$PATH"

# --- uv (Python package manager) ---
RUN curl -LsSf https://astral.sh/uv/install.sh | sh
ENV PATH="/root/.local/bin:$PATH"

ENV LD_LIBRARY_PATH=/root/app/clidriver/lib:/root/app/clidriver/lib/libdb2.so \
    PATH=$PATH:/root/app/clidriver/bin:/root/app/clidriver/lib:/usr/local/lib

