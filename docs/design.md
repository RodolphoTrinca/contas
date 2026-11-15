# Design Banco KRT v2

A referência é a biblioteca `design_system_component_library_v2_banco_krt` do arquivo `stitch_banco_krt_account_redesign.zip`. A v2 define os componentes; as outras telas do ZIP orientam a composição de cada fluxo. A interface não usa a versão simplificada anterior.

## Tokens e marca

No tema claro, os valores explícitos da seção **Tokens Visuais & Fundamentos** da v2 prevalecem sobre o `DESIGN.md` e sobre as configurações Tailwind divergentes do export:

| Token | Valor |
|---|---|
| Navy / hover | `#0A2540` / `#081D33` |
| Navy claro | `#E6EFF7` |
| Texto principal / secundário | `#0F172A` / `#334155` |
| Fundo / superfície de formulário | `#FAF8FF` / `#F2F3FF` |
| Sucesso | `#059669` sobre `#ECFDF5` |
| Exclusão | `#DC2626` sobre `#FEF2F2` |
| Campos / botões / cartões | 8 / 12 / 16 px |

A fonte Inter, sua licença, os tokens e o logo original referenciado pela v2 estão em `src/Contas.Web/public/brand`. A fonte e a marca são servidas localmente. O tema Keycloak usa os mesmos arquivos pelo Compose. Os ícones de interface são SVGs locais.

## Aparência clara, escura e do sistema

O seletor **Aparência**, disponível na entrada, no cabeçalho de contas e nas páginas do Keycloak, oferece **Sistema**, **Claro** e **Escuro**. Sistema é o padrão e acompanha mudanças da preferência do dispositivo. A escolha explícita prevalece sobre o sistema, permanece após recarregar e sincroniza entre abas.

O tema escuro mantém a composição, tipografia e geometria da v2, com superfícies navy e cores semânticas próprias:

| Uso | Cor no tema escuro |
|---|---|
| Fundo / superfície | `#080F1C` / `#101E31` |
| Texto principal / secundário | `#E8EEF7` / `#C1CCDC` |
| Ação primária / texto do botão | `#8DB9E8` / `#092039` |
| Destaque verde | `#74DDCB` |
| Sucesso | `#6EE7B7` sobre `#0D342D` |
| Erro | `#FECACA` sobre `#3A1B28` |

As duas paletas ficam em `src/Contas.Web/public/brand/tokens.css`. Os componentes usam tokens por função, como superfície, texto, ação e aviso. A marca original ganha uma apresentação monocromática clara por CSS no tema escuro. Listas, cartões, filtros, campos, diálogos, estados vazios e avisos usam a mesma aparência no desktop e no celular.

O script compartilhado `public/brand/theme.js` aplica `data-theme` ao documento antes da interface React carregar. A preferência usa a chave `krt-appearance` do `localStorage`; escolher Sistema remove a escolha explícita. Se o armazenamento estiver bloqueado, a troca funciona na página atual, sem persistência. O componente `ThemeSwitcher` acompanha esse estado sem divergências de hidratação.

Next.js e Keycloak recebem os mesmos tokens e script. A preferência atravessa login e saída porque ambos são servidos pela mesma origem pelo Nginx. Acesso direto por outra porta ou domínio tem armazenamento separado. O `darkMode=false` em `theme.properties` mantém desativado o mecanismo padrão do Keycloak; a aparência é controlada pelo script do Banco KRT para respeitar a escolha do usuário.

## Correspondência de telas

| Tela da aplicação | Referência no ZIP | Aplicação da v2 |
|---|---|---|
| Entrada, erro de autenticação e sessão expirada | `entrada_do_operador_pr_login` e variante mobile | Painel institucional navy, marca, card SSO, botão de acesso e avisos contextuais. |
| Login Keycloak e senha inválida | `tema_keycloak_login_confirma_o_de_sa_da` e `autentica_o_keycloak_logout_mobile` | Cabeçalho branco separado, identificação SSO, formulário claro, campos e feedback v2. |
| Confirmação de saída | Mesmas referências de autenticação | Card com resumo, retorno à aplicação e confirmação de saída. |
| Contas no desktop | `rea_de_trabalho_gest_o_de_contas_slide_over_sheet_moderno` | Cabeçalho institucional, contexto, atualização, tabela com avatares, CPF, status, ações e paginação. |
| Contas em tablet e celular | `gest_o_de_contas_mobile` e AccountCard da biblioteca v2 | Cartões, faixa de CPF, filtros por status, busca e ações de toque. |
| Abertura e edição | SlideOver da biblioteca v2 e `abrir_e_editar_conta_bottom_sheet_mobile` | Drawer navy no desktop, sheet claro no celular, modo ativo, contador de nome, CPF formatado, status e rodapé fixo. |
| Transferência de carteira e vínculo do titular | Componentes de formulário e SlideOver da biblioteca v2 | Mesma composição, conta selecionada, campo de identidade e explicação do efeito da operação. |
| Exclusão | ConfirmModal da biblioteca v2 e `confirma_o_de_exclus_o_mobile` | Ícone crítico, resumo real da conta, aviso e confirmação; diálogo central com botões empilhados no celular. |
| Lista vazia, filtros sem resultado, erro e acesso negado | Estados semânticos da biblioteca v2 | Ícones, superfícies e mensagens específicas, sem criar dados fictícios na interface. |

A lista se transforma em cartões abaixo de 960 px, usa duas colunas no tablet e uma no celular abaixo de 640 px. O formulário muda para bottom sheet em até 640 px. Dialogs limitam a altura à tela e rolam o conteúdo sem perder o rodapé. Os formulários têm foco inicial, Escape, foco contido e retorno ao acionador.

Busca e filtros operam sobre **a página carregada**, indicado no campo e nas contagens. O contrato atual não expõe busca global, total de contas, agência, saldo, extrato ou auditoria regulatória. Elementos ilustrativos dessas funções não são apresentados como serviços disponíveis. Os textos de segurança descrevem autenticação e permissões reais; não afirmam certificações, TLS local, biometria ou integrações Bacen inexistentes.

## Keycloak

O tema `banco-krt` herda os formulários nativos de `keycloak.v2`. O layout `template.ftl` deriva do Keycloak 26.6.2 e mantém scripts, mensagens, idiomas, provedores e fluxos de autenticação. Ao atualizar Keycloak, compare esse arquivo com o layout da nova versão. O formulário de saída preserva `session_code` e a ação fornecida pelo servidor.

O Compose monta a pasta completa do tema e os recursos compartilhados. Um realm novo recebe `banco-krt` pelo import; no realm existente, selecione **Realm settings → Themes → Login theme → banco-krt** e configure `pt-BR` na localização. A stack local já usa esse tema.

## Verificação

`bash scripts/test.sh e2e` executa os fluxos reais de login, CRUD, idempotência, carteiras e isolamento de clientes. `tests/e2e/design-v2.spec.ts` acrescenta inspeções de layout nos temas claro e escuro em 1440, 768, 390 e 320 px, máscaras, filtros, foco e estados de erro. `tests/e2e/theme.spec.ts` verifica a escolha de aparência, preferência do sistema, persistência, sincronização entre abas, armazenamento indisponível e continuidade pelo Keycloak. As capturas anexadas aos resultados permitem comparar cada tela com o export. Os dados fixos usados nesse teste visual são interceptados apenas no navegador de teste; os testes de negócio continuam usando PostgreSQL e Keycloak reais.
