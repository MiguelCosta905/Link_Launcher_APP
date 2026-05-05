using System;
using System.Collections.Generic;
using System.Globalization;

namespace LinkLauncher.App.Localization;

public static class UiText
{
    private const string DefaultLanguage = "pt-PT";

    private static readonly Dictionary<string, Dictionary<string, string>> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pt-PT"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tagline"] = "Minecraft, perfis e mods num só lugar",
            ["theme_label"] = "Tema",
            ["language_label"] = "Idioma",
            ["account_title"] = "Conta",
            ["player_label"] = "Jogador: {0}",
            ["login_microsoft"] = "Entrar com Microsoft",
            ["play_online"] = "Jogar Online",
            ["play_offline"] = "Jogar Offline",
            ["installations_title"] = "Instalações",
            ["new_installation"] = "Nova",
            ["copy_installation"] = "Copiar",
            ["delete_installation"] = "Apagar",
            ["open_installation_folder"] = "Abrir pasta da instalação",
            ["game_folder_title"] = "Pasta do jogo",
            ["open_game_folder"] = "Abrir pasta do jogo",
            ["current_installation"] = "Instalação atual",
            ["name_label"] = "Nome",
            ["minecraft_label"] = "Minecraft",
            ["instance_ram_label"] = "RAM da instância",
            ["releases"] = "Releases",
            ["snapshots"] = "Snapshots",
            ["old_beta"] = "Old Beta",
            ["old_alpha"] = "Old Alpha",
            ["loader_label"] = "Loader",
            ["loader_version_label"] = "Versão do loader",
            ["vanilla_loader_message"] = "Vanilla não usa loader",
            ["progress_title"] = "Progresso",
            ["files_label"] = "Ficheiros",
            ["download_label"] = "Download",
            ["mission_control_title"] = "Mission Control",
            ["events_title"] = "Erros e eventos",
            ["events_subtitle"] = "Seleciona um evento para ver detalhes técnicos.",
            ["theme_system"] = "Sistema",
            ["theme_light"] = "Claro",
            ["theme_dark"] = "Escuro",
            ["language_english"] = "English",
            ["language_portuguese"] = "Português (Portugal)",
            ["status_ready"] = "Launcher pronto.",
            ["status_settings_loaded"] = "Configuração carregada.",
            ["status_language_changed"] = "Idioma atualizado.",
            ["account_no_session"] = "Sem sessão Microsoft",
            ["account_session"] = "Sessão: {0}",
            ["instance_default_name"] = "Instância",
            ["instance_main_name"] = "Instância Principal",
            ["instance_numbered"] = "Instância {0}",
            ["copy_suffix"] = "cópia",
            ["modloader_vanilla"] = "Vanilla",
            ["modloader_no_version"] = "{0} sem versão definida",
            ["status_versions_loaded"] = "{0} versões carregadas.",
            ["status_no_versions_found"] = "Nenhuma versão encontrada.",
            ["status_loading_loader_versions"] = "A carregar versões compatíveis com {0}...",
            ["status_loader_versions_found"] = "{0} versões de {1} encontradas.",
            ["status_no_loader_versions_found"] = "Sem versões de {0} para Minecraft {1}.",
            ["operation_startup"] = "Arranque",
            ["status_opening_microsoft_login"] = "A abrir login Microsoft por código...",
            ["status_microsoft_login_done"] = "Login Microsoft concluído.",
            ["log_microsoft_code_received"] = "Código Microsoft recebido.",
            ["log_microsoft_login_started"] = "Login Microsoft iniciado.",
            ["log_signed_in_as"] = "Sessão iniciada como {0}",
            ["warning_login_before_online"] = "Faz login com a Microsoft antes de jogar online.",
            ["warning_online_blocked"] = "Arranque online bloqueado.",
            ["warning_no_active_session"] = "Não existe sessão Microsoft ativa.",
            ["status_preparing_launch"] = "A preparar arranque {0}...",
            ["warning_set_loader_version"] = "Define a versão do {0} antes de iniciar.",
            ["warning_loader_without_version"] = "Loader sem versão.",
            ["status_checking_java"] = "A verificar Java para {0}...",
            ["log_launch_started"] = "Arranque {0} iniciado.",
            ["status_launch_failed"] = "{0} falhou: {1}",
            ["log_launch_failed"] = "{0} falhou.",
            ["status_minecraft_started"] = "Minecraft iniciado. Processo: {0}",
            ["log_launch_success"] = "{0} iniciado.",
            ["log_game_folder_opened"] = "Pasta do jogo aberta.",
            ["log_installation_folder_opened"] = "Pasta da instalação aberta.",
            ["status_operation_failed"] = "{0} falhou: {1}",
            ["log_operation_failed"] = "{0} falhou.",
            ["log_process_ended"] = "Processo Minecraft {0} terminou.",
            ["status_keep_one_installation"] = "Mantém pelo menos uma instalação.",
            ["status_installation_created"] = "Instalação criada: {0}.",
            ["status_installation_copied"] = "Instalação duplicada: {0}.",
            ["status_installation_removed"] = "Instalação removida: {0}.",
            ["log_installation_created"] = "Instalação criada.",
            ["log_installation_copied"] = "Instalação duplicada.",
            ["log_installation_removed"] = "Instalação removida."
        },
        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tagline"] = "Minecraft, profiles and mods in one place",
            ["theme_label"] = "Theme",
            ["language_label"] = "Language",
            ["account_title"] = "Account",
            ["player_label"] = "Player: {0}",
            ["login_microsoft"] = "Sign in with Microsoft",
            ["play_online"] = "Play Online",
            ["play_offline"] = "Play Offline",
            ["installations_title"] = "Installations",
            ["new_installation"] = "New",
            ["copy_installation"] = "Copy",
            ["delete_installation"] = "Delete",
            ["open_installation_folder"] = "Open installation folder",
            ["game_folder_title"] = "Game folder",
            ["open_game_folder"] = "Open game folder",
            ["current_installation"] = "Current installation",
            ["name_label"] = "Name",
            ["minecraft_label"] = "Minecraft",
            ["instance_ram_label"] = "Instance RAM",
            ["releases"] = "Releases",
            ["snapshots"] = "Snapshots",
            ["old_beta"] = "Old Beta",
            ["old_alpha"] = "Old Alpha",
            ["loader_label"] = "Loader",
            ["loader_version_label"] = "Loader version",
            ["vanilla_loader_message"] = "Vanilla does not use a loader",
            ["progress_title"] = "Progress",
            ["files_label"] = "Files",
            ["download_label"] = "Download",
            ["mission_control_title"] = "Mission Control",
            ["events_title"] = "Errors and events",
            ["events_subtitle"] = "Select an event to inspect technical details.",
            ["theme_system"] = "System",
            ["theme_light"] = "Light",
            ["theme_dark"] = "Dark",
            ["language_english"] = "English",
            ["language_portuguese"] = "Portuguese (Portugal)",
            ["status_ready"] = "Launcher ready.",
            ["status_settings_loaded"] = "Settings loaded.",
            ["status_language_changed"] = "Language updated.",
            ["account_no_session"] = "No Microsoft session",
            ["account_session"] = "Session: {0}",
            ["instance_default_name"] = "Instance",
            ["instance_main_name"] = "Main Instance",
            ["instance_numbered"] = "Instance {0}",
            ["copy_suffix"] = "copy",
            ["modloader_vanilla"] = "Vanilla",
            ["modloader_no_version"] = "{0} with no version selected",
            ["status_versions_loaded"] = "{0} versions loaded.",
            ["status_no_versions_found"] = "No versions found.",
            ["status_loading_loader_versions"] = "Loading compatible versions for {0}...",
            ["status_loader_versions_found"] = "{0} {1} versions found.",
            ["status_no_loader_versions_found"] = "No {0} versions found for Minecraft {1}.",
            ["operation_startup"] = "Startup",
            ["status_opening_microsoft_login"] = "Opening Microsoft device code login...",
            ["status_microsoft_login_done"] = "Microsoft login completed.",
            ["log_microsoft_code_received"] = "Microsoft code received.",
            ["log_microsoft_login_started"] = "Microsoft login started.",
            ["log_signed_in_as"] = "Signed in as {0}",
            ["warning_login_before_online"] = "Sign in with Microsoft before playing online.",
            ["warning_online_blocked"] = "Online launch blocked.",
            ["warning_no_active_session"] = "There is no active Microsoft session.",
            ["status_preparing_launch"] = "Preparing {0} launch...",
            ["warning_set_loader_version"] = "Set the {0} version before launching.",
            ["warning_loader_without_version"] = "Loader without version.",
            ["status_checking_java"] = "Checking Java for {0}...",
            ["log_launch_started"] = "{0} launch started.",
            ["status_launch_failed"] = "{0} failed: {1}",
            ["log_launch_failed"] = "{0} failed.",
            ["status_minecraft_started"] = "Minecraft started. Process: {0}",
            ["log_launch_success"] = "{0} started.",
            ["log_game_folder_opened"] = "Game folder opened.",
            ["log_installation_folder_opened"] = "Installation folder opened.",
            ["status_operation_failed"] = "{0} failed: {1}",
            ["log_operation_failed"] = "{0} failed.",
            ["log_process_ended"] = "Minecraft {0} process ended.",
            ["status_keep_one_installation"] = "Keep at least one installation.",
            ["status_installation_created"] = "Installation created: {0}.",
            ["status_installation_copied"] = "Installation duplicated: {0}.",
            ["status_installation_removed"] = "Installation removed: {0}.",
            ["log_installation_created"] = "Installation created.",
            ["log_installation_copied"] = "Installation duplicated.",
            ["log_installation_removed"] = "Installation removed."
        }
    };

    public static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return DefaultLanguage;

        return Languages.ContainsKey(languageCode) ? languageCode : DefaultLanguage;
    }

    public static string Get(string? languageCode, string key)
    {
        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        if (Languages[normalizedLanguage].TryGetValue(key, out var value))
            return value;

        if (Languages[DefaultLanguage].TryGetValue(key, out value))
            return value;

        return key;
    }

    public static string Format(string? languageCode, string key, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, Get(languageCode, key), args);
    }
}
