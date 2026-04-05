#!/bin/bash

if [ ! -f "$HOME/.config/atomic.omp.json" ]; then
    echo "Configurando oh-my-posh ..."
    mv ~/.zshrc ~/.zshrc-old
    mkdir -p ~/.config && cp "$WORKFOLDER"/.devcontainer/atomic.omp.json ~/.config/atomic.omp.json
    cp "$WORKFOLDER"/.devcontainer/.zshrc ~/.zshrc
    # az config set extension.dynamic_install_allow_preview=false
    chmod -R 775 "$WORKFOLDER"
fi

if ! command -v oh-my-posh &> /dev/null; then
    echo "Instalando oh-my-posh ..."
    mkdir -p "$HOME/.local/bin"
    curl -fsSL https://cdn.ohmyposh.dev/releases/latest/posh-linux-amd64 -o "$HOME/.local/bin/oh-my-posh"
    chmod +x "$HOME/.local/bin/oh-my-posh"
fi
