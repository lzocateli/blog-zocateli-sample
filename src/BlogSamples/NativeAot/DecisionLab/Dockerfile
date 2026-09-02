FROM lzocateli/dotnet-sdk:10.0.400-noble

USER root

RUN apt-get update \
    && apt-get install --yes --no-install-recommends aspnetcore-runtime-8.0 clang git zlib1g-dev \
    && cp --archive /usr/lib/dotnet/shared/Microsoft.NETCore.App/8.0.* /usr/share/dotnet/shared/Microsoft.NETCore.App/ \
    && cp --archive /usr/lib/dotnet/shared/Microsoft.AspNetCore.App/8.0.* /usr/share/dotnet/shared/Microsoft.AspNetCore.App/ \
    && ln --symbolic --force /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip

ENTRYPOINT ["dotnet"]