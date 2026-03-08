-- ==========================================================================
-- Full-Text Search — PostgreSQL Setup
-- Artigo: Full-Text Search em API REST: C#, SQL Server e PostgreSQL
-- ==========================================================================

-- 1. Listar dicionários de busca textual instalados
SELECT cfgname, cfgparser::regproc
FROM pg_ts_config
ORDER BY cfgname;
-- Deve conter 'portuguese' para uso em português

-- 2. Habilitar extensão unaccent (inclusa no contrib)
CREATE EXTENSION IF NOT EXISTS unaccent;

-- 3. Criar configuração de texto que ignora acentos
CREATE TEXT SEARCH CONFIGURATION portuguese_unaccent (COPY = portuguese);
ALTER TEXT SEARCH CONFIGURATION portuguese_unaccent
  ALTER MAPPING FOR hword, hword_part, word
  WITH unaccent, portuguese_stem;

-- 4. Adicionar coluna tsvector para indexação
ALTER TABLE clientes ADD COLUMN busca_fts TSVECTOR;

-- 5. Popular a coluna com textos concatenados (pesos por campo)
UPDATE clientes SET busca_fts =
    setweight(to_tsvector('portuguese', coalesce(nome, '')),   'A') ||
    setweight(to_tsvector('portuguese', coalesce(email, '')),  'B') ||
    setweight(to_tsvector('portuguese', coalesce(cidade, '')), 'C');

-- 6. Trigger para manter a coluna atualizada automaticamente
CREATE OR REPLACE FUNCTION atualizar_busca_fts()
RETURNS TRIGGER AS $$
BEGIN
    NEW.busca_fts :=
        setweight(to_tsvector('portuguese', coalesce(NEW.nome,   '')), 'A') ||
        setweight(to_tsvector('portuguese', coalesce(NEW.email,  '')), 'B') ||
        setweight(to_tsvector('portuguese', coalesce(NEW.cidade, '')), 'C');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tg_clientes_busca_fts
BEFORE INSERT OR UPDATE ON clientes
FOR EACH ROW EXECUTE FUNCTION atualizar_busca_fts();

-- 7. Criar índice GIN (otimizado para tsvector)
CREATE INDEX idx_clientes_busca_fts ON clientes USING GIN (busca_fts);
