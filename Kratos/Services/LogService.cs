﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Kratos.Configs;

namespace Kratos.Services
{
    public class LogService
    {
        private DiscordSocketClient _client;

        public ulong ServerLogChannelId { get; set; }

        public ulong ModLogChannelId { get; set; }

        public bool EditsLogged { get; private set; }

        public bool DeletesLogged { get; private set; }

        public bool JoinsLogged { get; private set; }

        public bool LeavesLogged { get; private set; }

        public bool NameChangesLogged { get; private set; }

        public bool NickChangesLogged { get; private set; }

        public bool RoleUpdatesLogged { get; private set; }

        public bool BansLogged { get; private set; }

        #region Server Log Command Methods
        public void EnableEditLogging()
        {
            _client.MessageUpdated += _client_MessageUpdated;
            EditsLogged = true;
        }

        public void DisableEditLogging()
        {
            _client.MessageUpdated -= _client_MessageUpdated;
            EditsLogged = false;
        }

        public void EnableDeleteLogging()
        {
            _client.MessageDeleted += _client_MessageDeleted;
            DeletesLogged = true;
        }

        public void DisableDeleteLogging()
        {
            _client.MessageDeleted -= _client_MessageDeleted;
            DeletesLogged = false;
        }

        public void EnableJoinLogging()
        {
            _client.UserJoined += _client_UserJoined;
            JoinsLogged = true;
        }

        public void DisableJoinLogging()
        {
            _client.UserJoined -= _client_UserJoined;
            JoinsLogged = false;
        }

        public void EnableLeaveLogging()
        {
            _client.UserLeft += _client_UserLeft;
            LeavesLogged = true;
        }

        public void DisableLeaveLogging()
        {
            _client.UserLeft -= _client_UserLeft;
            LeavesLogged = false;
        }

        public void EnableNameChangeLogging()
        {
            _client.UserUpdated += _client_UserUpdated_NameChange;
            NameChangesLogged = true;
        }

        public void DisableNameChangeLogging()
        {
            _client.UserUpdated -= _client_UserUpdated_NameChange;
            NameChangesLogged = false;
        }

        public void EnableNickChangeLogging()
        {
            _client.GuildMemberUpdated += _client_GuildMemberUpdated_NickChange;
            NickChangesLogged = true;
        }

        public void DisableNickChangeLogging()
        {
            _client.GuildMemberUpdated -= _client_GuildMemberUpdated_NickChange;
            NickChangesLogged = false;
        }

        public void EnableRoleChangeLogging()
        {
            _client.GuildMemberUpdated += _client_GuildMemberUpdated_RoleChange;
            RoleUpdatesLogged = true;
        }

        public void DisableRoleChangeLogging()
        {
            _client.GuildMemberUpdated -= _client_GuildMemberUpdated_RoleChange;
            RoleUpdatesLogged = false;
        }

        public void EnableBanLogging()
        {
            _client.UserBanned += _client_UserBanned;
            BansLogged = true;
        }

        public void DisableBanLogging()
        {
            _client.UserBanned -= _client_UserBanned;
            BansLogged = false;
        }
        #endregion

        public async Task LogServerMessageAsync(string message)
        {
            if (ServerLogChannelId == 0) return;
            var channel = _client.GetChannel(ServerLogChannelId) as SocketTextChannel;
            await channel.SendMessageAsync(message.Replace("@", "(at)"));
        }

        public async Task LogModMessageAsync(string message)
        {
            if (ModLogChannelId == 0) return;
            var channel = _client.GetChannel(ModLogChannelId) as SocketTextChannel;
            await channel.SendMessageAsync(message);
        }

        public async Task<bool> SaveConfigurationAsync()
        {
            var config = new LogConfig(this);

            var serializedConfig = JsonConvert.SerializeObject(config);

            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "config", "log.json")))
                File.Create(Path.Combine(Directory.GetCurrentDirectory(), "config", "log.json")).Dispose();
            using (var configStream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "config", "log.json"), FileMode.Truncate))
            {
                using (var configWriter = new StreamWriter(configStream))
                {
                    await configWriter.WriteAsync(serializedConfig);
                    return true;
                }
            }
        }

        public async Task<bool> LoadConfigurationAsync()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "config", "log.json"))) return false;

            using (var configStream = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "config", "log.json")))
            {
                using (var configReader = new StreamReader(configStream))
                {
                    var serializedConfig = await configReader.ReadToEndAsync();
                    var config = JsonConvert.DeserializeObject<LogConfig>(serializedConfig);
                    if (config == null) return false;

                    ServerLogChannelId = config.ServerLog;
                    ModLogChannelId = config.ModLog;
                    EditsLogged = config.EditsLogged;
                    if (EditsLogged) EnableEditLogging();
                    DeletesLogged = config.DeletesLogged;
                    if (DeletesLogged) EnableDeleteLogging();
                    JoinsLogged = config.JoinsLogged;
                    if (JoinsLogged) EnableJoinLogging();
                    LeavesLogged = config.LeavesLogged;
                    if (LeavesLogged) EnableLeaveLogging();
                    NameChangesLogged = config.NameChangesLogged;
                    if (NameChangesLogged) EnableNameChangeLogging();
                    NickChangesLogged = config.NickChangesLogged;
                    if (NickChangesLogged) EnableNickChangeLogging();
                    RoleUpdatesLogged = config.RoleUpdatesLogged;
                    if (NickChangesLogged) EnableNickChangeLogging();
                    BansLogged = config.BansLogged;
                    if (BansLogged) EnableBanLogging();

                    return true;
                }
            }
        }

        #region Server Log Event Handlers
        private async Task _client_MessageUpdated(Cacheable<IMessage, ulong> b, SocketMessage a, ISocketMessageChannel channel)
        {
            if (a.Author.IsBot) return;

            var before = await b.GetOrDownloadAsync();
            if (before == null) return;
            if (before.Content == a.Content) return;
            var author = a.Author as SocketGuildUser;
            if (author == null) return;
            await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` {(a.Channel as SocketTextChannel).Mention} | **{author.Username}#{author.Discriminator} ({author.Id})** edited their message:\n" +
                                        $"**Before:** {before.Content}\n" + 
                                        $"**After:** {a.Content}");
        }

        private async Task _client_MessageDeleted(Cacheable<IMessage, ulong> m, ISocketMessageChannel channel)
        {
            var message = await m.GetOrDownloadAsync();
            if (message == null) return;
            if (message.Author.IsBot) return;
            var author = message.Author as SocketGuildUser;
            if (author == null) return;
            await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` {(channel as SocketTextChannel).Mention} | **{author.Username}#{author.Discriminator} ({author.Id})** deleted their message:\n" +
                                        $"{message.Content}");
        }

        private async Task _client_UserJoined(SocketGuildUser u)
        {
            var age = DateTime.UtcNow.Subtract(u.CreatedAt.UtcDateTime);
            if (age < TimeSpan.FromHours(1))
                await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` :new: {$"__**{u.Username}#{u.Discriminator} ({u.Id})**__ joined the server.".ToUpper()} :new: :bangbang: Account is *{age.Humanize(2)}* old.");
            else
                await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` :new: **{u.Username.ToUpper()}#{u.Discriminator} ({u.Id}) JOINED THE SERVER.** :new:");
        }

        private async Task _client_UserLeft(SocketGuildUser u)
        {
            await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` :wave: **{u.Username.ToUpper()}#{u.Discriminator} ({u.Id}) LEFT THE SERVER.** :wave:");
        }

        private async Task _client_UserUpdated_NameChange(SocketUser b, SocketUser a)
        {
            if (b.Username == a.Username) return;

            await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` :warning: **{b.Username}#{b.Discriminator} ({b.Id})** changed their username `{b.Username}` -> `{a.Username}`");
        }

        private async Task _client_GuildMemberUpdated_NickChange(SocketGuildUser b, SocketGuildUser a)
        {
            if (b.Nickname == a.Nickname) return;
            if (a.Nickname == null)
            {
                await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` :warning: **{b.Username}#{b.Discriminator} ({b.Nickname}) ({b.Id})** removed their nickname.");
                return;
            }
            await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` :warning: **{b.Nickname ?? b.Username}#{b.Discriminator} ({b.Id})** changed their nickname `{b.Nickname}` -> `{a.Nickname}`");
        }

        private async Task _client_GuildMemberUpdated_RoleChange(SocketGuildUser b, SocketGuildUser a)
        {
            if (b.Roles.Count() == a.Roles.Count()) return;
            await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` :warning: **{b.Username}#{b.Discriminator} ({b.Id})'s roles have changed:**\n" +
                                        $"**Before:** {string.Join(", ", b.Roles.Select(r => r.Name))}\n" +
                                        $"**After:** {string.Join(", ", a.Roles.Select(r => r.Name))}");
        }

        private async Task _client_UserBanned(SocketUser u, SocketGuild g)
        {
            await LogServerMessageAsync($"`{DateTime.UtcNow.ToString("[hh:mm:ss]")}` :no_entry_sign: **{u.Username.ToUpper()}#{u.Discriminator} ({u.Id}) WAS BANNED FROM THE SERVER.** :no_entry_sign:");
        }
        #endregion

        public LogService(DiscordSocketClient c)
        {
            _client = c;
        }
    }
}
