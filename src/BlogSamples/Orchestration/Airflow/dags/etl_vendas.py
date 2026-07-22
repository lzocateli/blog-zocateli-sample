# -----------------------------------------------------------------------
# Artigo: Apache Airflow com .NET 10: dispare e monitore DAGs
# URL: https://zocate.li/posts/2026/apache-airflow-dotnet-10-api-dags/
# DAG local usado para validar o cliente da API pública do Airflow 3.
# -----------------------------------------------------------------------

from datetime import datetime, timezone
from time import sleep

from airflow.sdk import dag, get_current_context, task


@dag(
    dag_id="etl_vendas",
    schedule=None,
    start_date=datetime(2026, 1, 1, tzinfo=timezone.utc),
    catchup=False,
    tags=["dotnet", "exemplo"],
)
def etl_vendas():
    @task
    def validar_parametros() -> dict[str, str]:
        contexto = get_current_context()
        configuracao = contexto["dag_run"].conf or {}
        data_referencia = configuracao.get("data_referencia")

        if not data_referencia:
            raise ValueError("O parâmetro data_referencia é obrigatório.")

        return {
            "data_referencia": str(data_referencia),
            "correlation_id": str(configuracao.get("correlation_id", "sem-correlacao")),
        }

    @task
    def processar_vendas(parametros: dict[str, str]) -> dict[str, object]:
        sleep(5)
        return {
            **parametros,
            "registros_processados": 1250,
            "valor_total": 98765.43,
        }

    @task
    def publicar_resumo(resultado: dict[str, object]) -> None:
        print(
            "ETL concluído: "
            f"correlation_id={resultado['correlation_id']}, "
            f"registros={resultado['registros_processados']}"
        )

    publicar_resumo(processar_vendas(validar_parametros()))


etl_vendas()
