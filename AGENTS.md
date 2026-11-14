# Contas

Leia README.md e a documentação da área que vai alterar. O código fica em src, a infraestrutura local em infra e os testes em tests.

- Preserve as dependências da Clean Architecture: Domain não referencia outras camadas; Application depende de Domain; Infrastructure implementa as portas de Application; Api faz a composição.
- Organize casos de uso por operação. Cada tipo C# fica em seu próprio arquivo. Regras do agregado e falhas esperadas retornam Result. Capture falhas de I/O na fronteira apropriada.
- Adicione versões NuGet em Directory.Packages.props. Regenere packages.lock.json com dotnet restore e valide com dotnet restore --locked-mode.
- Preserve o contrato da API v1, os códigos de erro, as permissões por endpoint e a gravação transacional da conta, recibo e outbox. Atualize OpenAPI e testes se o contrato mudar.
- Use dados fictícios. Não registre CPF, tokens, senhas ou corpos de requisição em logs. Nunca apague volumes fora de um projeto Compose descartável de testes.
- Rode bash scripts/test.sh fast e bash scripts/test.sh integration para domínio, arquitetura e integração. Rode os testes Playwright descritos no README quando alterar interface, login, proxy ou contrato HTTP.
