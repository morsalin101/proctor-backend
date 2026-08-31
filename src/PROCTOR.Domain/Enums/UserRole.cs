namespace PROCTOR.Domain.Enums;

public enum UserRole
{
    Student,
    Coordinator,
    Proctor,
    AssistantProctor,
    DeputyProctor,
    Registrar,
    DisciplinaryCommittee,
    FemaleCoordinator,
    SexualHarassmentCommittee,
    VC,
    SuperAdmin,

    /// <summary>
    /// A person outside the university who was called to a case hearing. The profile is
    /// created automatically when they are added to a hearing panel; they can sign in but
    /// only see the cases they are assigned to.
    /// </summary>
    External
}
