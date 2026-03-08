-- ==========================================================================
-- Full-Text Search — Oracle Text Setup
-- Artigo: Full-Text Search em API REST: C#, SQL Server e PostgreSQL
-- ==========================================================================

-- 1. Verificar se Oracle Text está instalado
SELECT comp_name, version, status
FROM   dba_registry
WHERE  comp_name = 'Oracle Text';
-- STATUS deve ser 'VALID'

-- 2. Instalar Oracle Text (se necessário — executar como SYS)
-- @?/ctx/admin/catctx.sql ctxsys SYSAUX TEMP NOLOCK
-- @?/rdbms/admin/utlrp.sql

-- 3. Conceder permissões ao usuário da aplicação
-- Opção 1: role CTXAPP
GRANT CTXAPP TO meu_usuario;

-- Opção 2: privilégios granulares (produção)
GRANT EXECUTE ON CTXSYS.CTX_DDL   TO meu_usuario;
GRANT EXECUTE ON CTXSYS.CTX_QUERY TO meu_usuario;

-- 4. Verificar processo extproc (necessário para sincronizador)
SELECT program, status FROM v$process WHERE program LIKE '%extproc%';

-- 5. Criar preferências de idioma
BEGIN
  CTX_DDL.CREATE_PREFERENCE('pref_clientes', 'BASIC_LEXER');
  CTX_DDL.SET_ATTRIBUTE('pref_clientes', 'BASE_LETTER', 'YES');  -- ignora acentos
  CTX_DDL.SET_ATTRIBUTE('pref_clientes', 'MIXED_CASE', 'NO');    -- case insensitive

  CTX_DDL.CREATE_PREFERENCE('wordlist_clientes', 'BASIC_WORDLIST');
  CTX_DDL.SET_ATTRIBUTE('wordlist_clientes', 'PREFIX_INDEX', 'YES');
  CTX_DDL.SET_ATTRIBUTE('wordlist_clientes', 'PREFIX_MIN_LENGTH', '2');
  CTX_DDL.SET_ATTRIBUTE('wordlist_clientes', 'PREFIX_MAX_LENGTH', '10');
END;
/

-- 6. Criar índice CONTEXT
CREATE INDEX idx_clientes_fts ON clientes(nome)
INDEXTYPE IS CTXSYS.CONTEXT
PARAMETERS ('LEXER pref_clientes WORDLIST wordlist_clientes');

-- 7. Sincronizar o índice após inserções/updates
EXEC CTX_DDL.SYNC_INDEX('idx_clientes_fts');

-- Para auto-sincronização (Oracle 12c+):
-- CREATE INDEX idx_clientes_fts ON clientes(nome)
-- INDEXTYPE IS CTXSYS.CONTEXT
-- PARAMETERS ('... SYNC (ON COMMIT)');
