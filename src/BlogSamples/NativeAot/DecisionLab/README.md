# DecisionLab: JIT, ReadyToRun e Native AOT

Laboratório reproduzível do artigo [.NET Native AOT: Parte 2 — JIT, R2R e AOT em Benchmarks](https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/). O mesmo workload determinístico é usado pela Minimal API, pelo BenchmarkDotNet e pela carga HTTP.

## O que é medido

| Instrumento | Escopo | Saída |
| --- | --- | --- |
| `DecisionLab.Harness` | publish limpo/incremental, tamanho e startup até `/ready` | CSV e JSON brutos + manifesto |
| BenchmarkDotNet | CPU e alocações do workload isolado em JIT e Native AOT | Markdown, CSV e JSON |
| Microsoft Crank + Bombardier | throughput, latência, CPU e working set da API | JSON por rodada |

Os quatro perfis usam `linux-x64`, Release, Production e o mesmo código:

- `jit-fdd`: framework-dependent com runtime compartilhado instalado no host.
- `jit-scd`: self-contained com CoreCLR e JIT no diretório publicado.
- `r2r-scd`: self-contained com ReadyToRun, tiering e JIT disponíveis.
- `native-aot`: self-contained compilado pelo ILCompiler, sem JIT em produção.

## Pré-requisitos

- Docker Desktop com containers Linux.
- `uv` para executar a consolidação Python.
- Microsoft Crank Controller `0.2.0-alpha.26316.1` no host.
- Dois agentes Crank para a carga HTTP. Usar máquinas separadas é preferível; o perfil `local` mede tudo no mesmo host e contamina capacidade absoluta. A imagem inclui ASP.NET Core Runtime 8, exigido pela versão atual do agent, sem alterar o runtime .NET 10 das aplicações medidas.

## Build do ambiente

Execute na raiz do repositório:

```bash
docker build -t decision-lab:10.0.400 src/BlogSamples/NativeAot/DecisionLab
```

## Coletar publish, tamanho e startup

O harness restaura antes do cronômetro, faz três pares de publish limpo/incremental e inicia cada perfil 30 vezes em ordem randomizada. Antes dos startups, ele copia os artefatos do bind mount para o filesystem local do container para não medir o compartilhamento de arquivos do Docker Desktop. A métrica é startup de processo com cache do sistema de arquivos aquecido, não cold boot da máquina nem cold start de provedor serverless.

```bash
docker run --rm \
  --volume "$PWD:/workspace" \
  --volume decision-lab-nuget:/root/.nuget/packages \
  decision-lab:10.0.400 \
  run --project src/BlogSamples/NativeAot/DecisionLab/DecisionLab.Harness \
  --configuration Release -- \
  --repository-root /workspace
```

Para um smoke test, reduza ambas as repetições para 1. Não publique esses números:

```bash
docker run --rm --volume "$PWD:/workspace" --volume decision-lab-nuget:/root/.nuget/packages decision-lab:10.0.400 \
  run --project src/BlogSamples/NativeAot/DecisionLab/DecisionLab.Harness \
  --configuration Release -- \
  --repository-root /workspace --publish-repetitions 1 --startup-repetitions 1
```

Para repetir somente startup com os quatro diretórios já publicados, acrescente `--skip-publish`.

## Executar o microbenchmark

```bash
docker run --rm --volume "$PWD:/workspace" --volume decision-lab-nuget:/root/.nuget/packages decision-lab:10.0.400 \
  run --project src/BlogSamples/NativeAot/DecisionLab/DecisionLab.Benchmarks \
  --configuration Release -- --filter '*WorkloadBenchmarks*'
```

Os atributos do benchmark criam jobs out-of-process distintos para `.NET 10.0` e `NativeAOT 10.0`; métodos com nomes diferentes não são usados para simular runtimes.

## Executar carga HTTP com Crank

Primeiro execute o harness para produzir os quatro diretórios em `artifacts/publish`. Inicie um `Microsoft.Crank.Agent` na porta 5010 para a aplicação e outro na 5011 para o Bombardier. O container da aplicação também publica a porta 5000 para que o gerador de carga no segundo container alcance a API:

```powershell
docker run -d --rm --name decision-lab-crank-app `
  -p 5010:5010 -p 5000:5000 `
  --volume "${PWD}:/workspace" `
  --volume decision-lab-nuget:/root/.nuget/packages `
  --entrypoint bash decision-lab:10.0.400 -lc `
  "dotnet tool install Microsoft.Crank.Agent --tool-path /tools --version '0.2.0-*' >/dev/null && /tools/crank-agent --url http://0.0.0.0:5010 --hostname host.docker.internal --build-path /tmp/crank-app"

docker run -d --rm --name decision-lab-crank-load `
  -p 5011:5011 `
  --volume "${PWD}:/workspace" `
  --volume decision-lab-nuget:/root/.nuget/packages `
  --entrypoint bash decision-lab:10.0.400 -lc `
  "dotnet tool install Microsoft.Crank.Agent --tool-path /tools --version '0.2.0-*' >/dev/null && /tools/crank-agent --url http://0.0.0.0:5011 --hostname host.docker.internal --build-path /tmp/crank-load"
```

Espere os dois logs exibirem `Agent ready, waiting for jobs`. Em seguida, rode cinco vezes por perfil, preservando cada JSON. Use caminhos Linux absolutos nos argumentos enviados ao agent; caminhos relativos são normalizados para o diretório temporário do job:

```bash
crank --config src/BlogSamples/NativeAot/DecisionLab/crank/decision-lab.benchmarks.yml \
  --scenario workload --profile local \
  --variable executable=/usr/share/dotnet/dotnet \
  --variable applicationArguments=/workspace/src/BlogSamples/NativeAot/DecisionLab/artifacts/publish/jit-fdd/DecisionLab.Api.dll \
  --json results/crank-jit-fdd-01.json
```

Para `jit-scd`, `r2r-scd` e `native-aot`, defina `executable` como o caminho absoluto `/workspace/.../<perfil>/DecisionLab.Api` e deixe `applicationArguments` vazio. O runner executa cinco rodadas de cada perfil, interrompe na primeira resposta inválida e grava a ordem reproduzível em `crank-order.json`:

```powershell
./src/BlogSamples/NativeAot/DecisionLab/scripts/run_crank_campaign.ps1 `
  -CrankPath "$env:TEMP/decision-lab-crank-tools/crank.exe" `
  -ResultsDirectory ./src/BlogSamples/NativeAot/DecisionLab/results/<ambiente>/<data> `
  -Seed 20260901
```

Conexões, warm-up, duração e payload estão fixados no YAML. Descarte a campanha inteira se qualquer rodada registrar respostas inválidas.

Cada JSON bruto registra a versão completa do Crank Agent, incluindo o commit do pacote. O cenário atual importa o job Bombardier da branch `main`; para repetir a campanha em outra data com a mesma implementação do gerador, substitua esse import por uma URL pinada em um commit imutável antes de iniciar qualquer rodada.

## Consolidar os resultados

```bash
uv run python src/BlogSamples/NativeAot/DecisionLab/scripts/consolidate_results.py \
  src/BlogSamples/NativeAot/DecisionLab/results/<ambiente>/<data>
```

O script exige cinco rodadas Crank válidas por perfil e resultados completos dos jobs JIT e Native AOT do BenchmarkDotNet. Depois calcula mediana, mínimo, máximo, desvio absoluto mediano e razão contra `jit-fdd`. Diferenças de até 3% são marcadas como dentro do limiar de ruído; o artigo deve declarar o ambiente e não transformar esse limiar em significância estatística.

## Contrato de evidência

- Não editar números consolidados manualmente.
- Manter os dados brutos, o `manifest.json` e o resumo no mesmo diretório versionado.
- Não misturar restore com publish, startup com warm-up ou FDD app-only com distribuições autocontidas sem ressalva.
- Registrar falhas e timeouts; não removê-los silenciosamente.
- Não generalizar resultados deste workload sintético para outra aplicação, RID, hardware ou provedor.
