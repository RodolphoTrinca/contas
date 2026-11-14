# Design Banco KRT

A interface responsiva usa a paleta e os componentes do export Stitch Banco KRT Enterprise Operations. Os tokens, o símbolo e a fonte Inter local ficam em `src/Contas.Web/public/brand` e são compartilhados com o tema do Keycloak pelo Compose. A licença da fonte acompanha o arquivo.

A listagem usa uma tabela em desktop e cartões em telas menores. Abertura, edição e atribuição usam um painel lateral no desktop e um painel inferior no celular. A exclusão usa um diálogo com confirmação explícita. A interface exibe CPF mascarado na lista e só mostra controles de escrita quando a API autoriza o perfil.

O tema `banco-krt` personaliza o login e a confirmação de saída, preservando os formulários e as ações do Keycloak. Um realm novo recebe o tema pelo arquivo de importação. Em um realm já existente, após atualizar a stack, selecione `banco-krt` em **Realm settings → Themes → Login theme**. Para português brasileiro, configure `pt-BR` na seção de localização do realm.

O export de referência contém campos e textos ilustrativos. A interface usa os campos da API de Contas e os estados reais de autenticação, permissão, carregamento e erro. Os testes em `tests/e2e/responsive.spec.ts` verificam o login, o layout móvel e as operações de conta sem rolagem horizontal.
