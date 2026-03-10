namespace ZapChat.Api.Data;

using Microsoft.EntityFrameworkCore;
using ZapChat.Api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<MessageReadReceipt> MessageReadReceipts => Set<MessageReadReceipt>();
    public DbSet<Friend> Friends => Set<Friend>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.HasCharSet("utf8mb4");

        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Bio).HasMaxLength(200);
        });

        b.Entity<Conversation>(e =>
        {
            e.HasIndex(c => c.LastActivityAt);

            e.HasOne(c => c.CreatedBy)
             .WithMany()
             .HasForeignKey(c => c.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.LastMessage)
             .WithMany()
             .HasForeignKey(c => c.LastMessageId)
             .OnDelete(DeleteBehavior.NoAction)
             .IsRequired(false);
        });

        b.Entity<ConversationMember>(e =>
        {
            e.HasIndex(cm => new { cm.ConversationId, cm.UserId }).IsUnique();

            e.HasOne(cm => cm.Conversation)
             .WithMany(c => c.Members)
             .HasForeignKey(cm => cm.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(cm => cm.User)
             .WithMany(u => u.ConversationMembers)
             .HasForeignKey(cm => cm.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Message>(e =>
        {
            e.HasIndex(m => new { m.ConversationId, m.SentAt });

            e.HasOne(m => m.Conversation)
             .WithMany(c => c.Messages)
             .HasForeignKey(m => m.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Sender)
             .WithMany(u => u.Messages)
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.ReplyTo)
             .WithMany(m => m.Replies)
             .HasForeignKey(m => m.ReplyToId)
             .OnDelete(DeleteBehavior.NoAction)
             .IsRequired(false);
        });

        b.Entity<MessageAttachment>(e =>
        {
            e.HasOne(a => a.Message)
             .WithMany(m => m.Attachments)
             .HasForeignKey(a => a.MessageId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MessageReadReceipt>(e =>
        {
            e.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();

            e.HasOne(r => r.Message)
             .WithMany(m => m.ReadReceipts)
             .HasForeignKey(r => r.MessageId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MessageReaction>(e =>
        {
            e.HasIndex(r => new { r.MessageId, r.UserId, r.Emoji }).IsUnique();

            e.HasOne(r => r.Message)
             .WithMany(m => m.Reactions)
             .HasForeignKey(r => r.MessageId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.User)
             .WithMany(u => u.Reactions)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Friend>(e =>
        {
            e.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
            e.HasIndex(f => new { f.AddresseeId, f.Status });

            e.HasOne(f => f.Requester)
             .WithMany(u => u.SentFriendRequests)
             .HasForeignKey(f => f.RequesterId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(f => f.Addressee)
             .WithMany(u => u.ReceivedFriendRequests)
             .HasForeignKey(f => f.AddresseeId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}