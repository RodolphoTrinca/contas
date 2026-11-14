<#import "template.ftl" as layout>
<@layout.registrationLayout; section>
    <#if section = "header">
        ${msg("logoutConfirmTitle")}
    <#elseif section = "form">
        <div id="kc-logout-confirm" class="content-area">
            <div class="krt-logout-message"><span class="krt-logout-icon" aria-hidden="true">↪</span><div><h2>Encerrar sessão</h2><p class="instruction">${msg("logoutConfirmHeader")}</p></div></div>
            <p class="krt-logout-note">Para acessar suas contas novamente, entre com sua credencial.</p>
            <form class="form-actions" action="${url.logoutConfirmAction}" onsubmit="confirmLogout.disabled = true; return true;" method="POST">
                <input type="hidden" name="session_code" value="${logoutConfirm.code}">
                <div class="krt-logout-actions"><a class="krt-secondary-button" href="/">Voltar à aplicação</a><input class="${properties.kcButtonClass!} ${properties.kcButtonPrimaryClass!}" name="confirmLogout" id="kc-logout" type="submit" value="${msg("doLogout")}" /></div>
            </form>
        </div>
    </#if>
</@layout.registrationLayout>
