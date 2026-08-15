using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

using Schala.Utility;

namespace Schala;

public class AdminModule
{
    private static readonly (DiscordPermission Permission, string Label)[] TrackedPermissions =
    [
        (DiscordPermission.AttachFiles, "Attach Files"),
        (DiscordPermission.ReadMessageHistory, "Read Message History"),
        (DiscordPermission.MentionEveryone, "Mention Everyone"),
        (DiscordPermission.UseExternalEmojis, "Use External Emojis"),
        (DiscordPermission.Connect, "Connect"),

        (DiscordPermission.Speak, "Speak"),
        (DiscordPermission.MuteMembers, "Mute Members"),
        (DiscordPermission.MoveMembers, "Move Members"),
        (DiscordPermission.EmbedLinks, "Embed Links"),
        (DiscordPermission.UseVoiceActivity, "Use Voice Activation"),

        (DiscordPermission.PrioritySpeaker, "Priority Speaker"),
        (DiscordPermission.ChangeNickname, "Change Nickname"),
        (DiscordPermission.ManageNicknames, "Manage Nicknames"),
        (DiscordPermission.ManageRoles, "Manage Roles"),
        (DiscordPermission.DeafenMembers, "Deafen Members"),

        (DiscordPermission.ManageMessages, "Manage Messages"),
        (DiscordPermission.ViewChannel, "View Channel"),
        (DiscordPermission.SendMessages, "Send Messages"),
        (DiscordPermission.CreateInvite, "Create Instant Invite"),
        (DiscordPermission.BanMembers, "Ban Members"),

        (DiscordPermission.SendTtsMessages, "Send TTS Messages"),
        (DiscordPermission.Administrator, "Administrator"),
        (DiscordPermission.KickMembers, "Kick Members"),
        (DiscordPermission.ManageGuild, "Manage Guild"),
        (DiscordPermission.AddReactions, "Add Reactions"),

        (DiscordPermission.ViewAuditLog, "View Audit Log"),
        (DiscordPermission.ManageWebhooks, "Manage Webhooks"),
        (DiscordPermission.ManageChannels, "Manage Channels"),
        (DiscordPermission.ManageGuildExpressions, "Manage Emojis"),
    ];

    [Command("permissions")]
    public static async Task CheckPermissions(CommandContext ctx, DiscordMember User, DiscordChannel Channel)
    {
        DiscordEmbedBuilder output = new DiscordEmbedBuilder()
            .WithAuthor($"{User.Username} : {User.Nickname ?? User.Username}")
            .WithTitle($"Permissions in #{Channel.Name}");

        DiscordPermissions effective = Channel.PermissionsFor(User);

        StringBuilder sb = new();
        foreach (var (permission, label) in TrackedPermissions)
        {
            string grantingRoles = string.Join(", ", User.Roles.Where(r => r.Permissions.HasPermission(permission)).Select(r => r.Name));
            if (grantingRoles.Length == 0)
                grantingRoles = "(none)";

            sb.AppendLine($"**{label}:** {(effective.HasPermission(permission) ? "✅" : "❌")} <- {grantingRoles}");
        }

        output.AddField("Permissions", sb.ToString());

        if (ctx is SlashCommandContext slashCtx)
            await slashCtx.RespondAsync(output.Build(), true);
        else
            await ctx.RespondAsync(output.Build());
    }
}
