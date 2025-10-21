using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public class GameTurnEntity
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public int TurnNumber { get; set; }
        public DateTime FinishedAt { get; set; }
        public List<PlayerTurnResult> Players { get; set; } = new();
        public Guid? WinnerId { get; set; }
        
        public GameTurnEntity(Guid gameId, int turnNumber)
        {
            Id = Guid.NewGuid();
            GameId = gameId;
            TurnNumber = turnNumber;
            FinishedAt = DateTime.UtcNow;
        }
    }
    
    public class PlayerTurnResult
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public PlayerDecision Decision { get; set; }
        public TurnResult Result { get; set; }
    }
    
    public enum TurnResult
    {
        Lost,
        Won,
        Draw,
    }
}