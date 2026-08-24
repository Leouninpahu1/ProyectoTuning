using Microsoft.EntityFrameworkCore;
using Turning.Domain.Entities;

namespace Turning.Infrastructure.Persistence;

/// <summary>
/// DbContext principal de la solución.
/// </summary>
public sealed class TurningDbContext : DbContext
{
    /// <summary>
    /// Constructor del contexto.
    /// </summary>
    public TurningDbContext(DbContextOptions<TurningDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Usuarios autenticables persistidos.
    /// </summary>
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    /// <summary>
    /// Sesiones experimentales persistidas.
    /// </summary>
    public DbSet<ExperimentSession> ExperimentSessions => Set<ExperimentSession>();

    /// <summary>
    /// Turnos de conversación persistidos.
    /// </summary>
    public DbSet<ConversationTurn> ConversationTurns => Set<ConversationTurn>();

    public DbSet<SessionAuditEntry> SessionAuditEntries => Set<SessionAuditEntry>();
    public DbSet<ConditionAssignment> ConditionAssignments => Set<ConditionAssignment>();
    public DbSet<EmotionReading> EmotionReadings => Set<EmotionReading>();
    public DbSet<AvatarExpression> AvatarExpressions => Set<AvatarExpression>();
    public DbSet<SurveyDefinition> SurveyDefinitions => Set<SurveyDefinition>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<ExperimentEvent> ExperimentEvents => Set<ExperimentEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("UserAccounts");

            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).IsRequired().HasMaxLength(200);
            entity.Property(user => user.NormalizedEmail).IsRequired().HasMaxLength(200);
            entity.Property(user => user.FullName).IsRequired().HasMaxLength(200);
            entity.Property(user => user.PasswordHash).IsRequired().HasMaxLength(1000);
            entity.Property(user => user.Role).IsRequired().HasMaxLength(50);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<ExperimentSession>(entity =>
        {
            entity.ToTable("ExperimentSessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.SessionCode).IsRequired().HasMaxLength(20);
            entity.Property(session => session.OwnerUserId).IsRequired();
            entity.Property(session => session.Condition).HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(session => session.Status).HasConversion<string>().IsRequired().HasMaxLength(30);
            entity.Property(session => session.AvatarState).IsRequired().HasMaxLength(50);
            entity.Property(session => session.LastDetectedEmotion).HasMaxLength(50);
            entity.Property(session => session.ActivatedAtUtc);
            entity.Property(session => session.ExpiresAtUtc);
            entity.Property(session => session.LastActivityAtUtc);
            entity.Property(session => session.CompletedAtUtc);
            entity.Property(session => session.CancelledAtUtc);
            entity.Property(session => session.CancellationReason).HasMaxLength(500);
            var rowVersion = entity.Property(session => session.RowVersion)
                .IsConcurrencyToken();
            if (Database.IsSqlServer())
                rowVersion.IsRowVersion();
            else
                rowVersion.ValueGeneratedNever().HasDefaultValue(new byte[] { 0 });
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(session => session.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(session => session.SessionCode).IsUnique();
            entity.HasIndex(session => new { session.OwnerUserId, session.CreatedAt });
            entity.HasIndex(session => new { session.Status, session.ActivatedAtUtc });
        });

        modelBuilder.Entity<SessionAuditEntry>(entity =>
        {
            entity.ToTable("SessionAuditEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired();
            entity.Property(e => e.PreviousStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.NewStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.ActorType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.MetadataJson).HasMaxLength(2000);
            entity.HasIndex(e => new { e.SessionId, e.OccurredAtUtc });
            entity.HasOne<ExperimentSession>().WithMany().HasForeignKey(e => e.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationTurn>(entity =>
        {
            entity.ToTable("ConversationTurns");
            entity.HasKey(turn => turn.Id);
            entity.Property(turn => turn.ExperimentSessionId).IsRequired();
            entity.Property(turn => turn.SequenceNumber).IsRequired();
            entity.Property(turn => turn.Sender).HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(turn => turn.Message).IsRequired().HasMaxLength(4000);
            entity.Property(turn => turn.OriginatingTurnId);
            entity.HasIndex(turn => new { turn.ExperimentSessionId, turn.SequenceNumber }).IsUnique();
            entity.HasOne<ExperimentSession>().WithMany().HasForeignKey(turn => turn.ExperimentSessionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ConditionAssignment>(e=>{e.ToTable("ConditionAssignments");e.HasKey(x=>x.Id);e.Property(x=>x.SessionId).IsRequired();e.HasIndex(x=>x.SessionId).IsUnique();e.Property(x=>x.Strategy).HasMaxLength(50);e.Property(x=>x.Reason).HasMaxLength(500);e.Property(x=>x.Condition).HasConversion<string>().HasMaxLength(20);e.HasOne<ExperimentSession>().WithMany().HasForeignKey(x=>x.SessionId).OnDelete(DeleteBehavior.Cascade);});
        modelBuilder.Entity<EmotionReading>(e=>{e.ToTable("EmotionReadings");e.HasKey(x=>x.Id);e.Property(x=>x.Emotion).HasMaxLength(50);e.Property(x=>x.Source).HasMaxLength(30);e.Property(x=>x.Provider).HasMaxLength(100);e.HasIndex(x=>new{x.SessionId,x.CapturedAtUtc});e.HasOne<ExperimentSession>().WithMany().HasForeignKey(x=>x.SessionId).OnDelete(DeleteBehavior.Cascade);e.HasOne<ConversationTurn>().WithMany().HasForeignKey(x=>x.ConversationTurnId).OnDelete(DeleteBehavior.NoAction);});
        modelBuilder.Entity<AvatarExpression>(e=>{e.ToTable("AvatarExpressions");e.HasKey(x=>x.Id);e.Property(x=>x.ExpressionName).HasMaxLength(50);e.Property(x=>x.ParametersJson).HasMaxLength(2000);e.HasIndex(x=>new{x.SessionId,x.CreatedAt});e.HasOne<ExperimentSession>().WithMany().HasForeignKey(x=>x.SessionId).OnDelete(DeleteBehavior.Cascade);e.HasOne<EmotionReading>().WithMany().HasForeignKey(x=>x.EmotionReadingId).OnDelete(DeleteBehavior.NoAction);});
        modelBuilder.Entity<SurveyDefinition>(e=>{e.ToTable("SurveyDefinitions");e.HasKey(x=>x.Id);e.Property(x=>x.Code).HasMaxLength(50);e.Property(x=>x.Version).HasMaxLength(20);e.Property(x=>x.Name).HasMaxLength(200);e.HasIndex(x=>x.Code).IsUnique();});
        modelBuilder.Entity<SurveyQuestion>(e=>{e.ToTable("SurveyQuestions");e.HasKey(x=>x.Id);e.Property(x=>x.Code).HasMaxLength(50);e.Property(x=>x.Text).HasMaxLength(1000);e.Property(x=>x.Type).HasMaxLength(30);e.HasIndex(x=>new{x.SurveyDefinitionId,x.Order}).IsUnique();e.HasOne<SurveyDefinition>().WithMany(x=>x.Questions).HasForeignKey(x=>x.SurveyDefinitionId).OnDelete(DeleteBehavior.Cascade);});
        modelBuilder.Entity<SurveyResponse>(e=>{e.ToTable("SurveyResponses");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.SessionId,x.SurveyDefinitionId}).IsUnique();e.HasOne<ExperimentSession>().WithMany().HasForeignKey(x=>x.SessionId).OnDelete(DeleteBehavior.Cascade);e.HasOne<SurveyDefinition>().WithMany().HasForeignKey(x=>x.SurveyDefinitionId).OnDelete(DeleteBehavior.Restrict);e.HasOne<UserAccount>().WithMany().HasForeignKey(x=>x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<SurveyAnswer>(e=>{e.ToTable("SurveyAnswers");e.HasKey(x=>x.Id);e.Property(x=>x.Value).HasMaxLength(4000);e.HasIndex(x=>new{x.SurveyResponseId,x.SurveyQuestionId}).IsUnique();e.HasOne<SurveyResponse>().WithMany(x=>x.Answers).HasForeignKey(x=>x.SurveyResponseId).OnDelete(DeleteBehavior.Cascade);e.HasOne<SurveyQuestion>().WithMany().HasForeignKey(x=>x.SurveyQuestionId).OnDelete(DeleteBehavior.Restrict);});
        modelBuilder.Entity<ExperimentEvent>(e=>{e.ToTable("ExperimentEvents");e.HasKey(x=>x.Id);e.Property(x=>x.Type).HasMaxLength(50);e.Property(x=>x.PayloadJson).HasMaxLength(4000);e.HasIndex(x=>new{x.SessionId,x.OccurredAtUtc});e.HasOne<ExperimentSession>().WithMany().HasForeignKey(x=>x.SessionId).OnDelete(DeleteBehavior.Cascade);});
        base.OnModelCreating(modelBuilder);
    }
}
