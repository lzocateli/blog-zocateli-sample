-- ==========================================================================
-- Full-Text Search — SQL Server Setup
-- Artigo: Full-Text Search em API REST: C#, SQL Server e PostgreSQL
-- ==========================================================================

-- 1. Verificar se FTS está instalado
SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') AS FTInstalled;

-- 2. Criar o catálogo (uma vez por banco)
CREATE FULLTEXT CATALOG catalogo_clientes AS DEFAULT;

-- 3. Criar o índice FT na tabela Clientes
--    Requer que a tabela tenha uma coluna chave única (geralmente PK)
CREATE FULLTEXT INDEX ON Clientes
(
    Nome          LANGUAGE 'Brazilian',
    Email         LANGUAGE 'Brazilian',
    Cidade        LANGUAGE 'Brazilian'
)
KEY INDEX PK_Clientes
ON catalogo_clientes
WITH STOPLIST = SYSTEM;

-- 4. Verificar progresso da indexação
SELECT name, is_enabled, crawl_status_description
FROM sys.fulltext_indexes fi
JOIN sys.tables t ON fi.object_id = t.object_id;

-- 5. Exemplo de query CONTAINS com prefixo
SELECT Id, Nome, CpfCnpj, Email, Cidade
FROM Clientes
WHERE CONTAINS((Nome, Email, Cidade), '"João*" AND "Si*"')
ORDER BY Nome
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;
