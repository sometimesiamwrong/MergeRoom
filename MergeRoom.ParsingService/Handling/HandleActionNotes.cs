namespace MergeRoom.Parsing.Handling
{
    public static class HandleActionNotes
    {
        public static Dictionary<BaseEntity, ExecuteActionTypes> Handle(HandleData data, DiscordBotConfiguration configuration)
        {
            var actions = new Dictionary<BaseEntity, ExecuteActionTypes>();

            foreach (var note in data.Notes)
            {
                if (data.NewMergeRequest.State == "closed" || data.NewMergeRequest.State == "merged")
                {
                    continue;
                }

                var action = ExecuteActionTypes.NoteNew;
                note.NoteType = NoteType.Basic;

                if (note.IsSystem)
                {
                    if (note.Description.Contains(configuration.PossibleSystemNoteKinds.ResolvedAllThreads))
                    {
                        note.NoteType = NoteType.ResolvedAllThreads;
                    }
                    else if (note.Description.Contains(configuration.PossibleSystemNoteKinds.Unapprove))
                    {
                        note.NoteType = NoteType.Unapprove;
                    }
                    else if (note.Description.Contains(configuration.PossibleSystemNoteKinds.Approve))
                    {
                        note.NoteType = NoteType.Approve;
                    }
                    if (note.Description.Contains("JobToNoteConverted"))
                    {
                        var job = note.Data as Job;
                        action = ExecuteActionTypes.Unknown;
                        if (job!.Status == "failed" && job.AllowFailure == false)
                        {
                            note.NoteType = NoteType.JobFailed;
                            action = ExecuteActionTypes.NoteNew;
                        }
                    }
                }
                else
                {
                    if (note is DiffNote diffNote)
                    {
                        diffNote.NoteType = NoteType.Diff;
                    }
                }

                actions.Add(note, action);
            }

            return actions;
        }
    }
}
