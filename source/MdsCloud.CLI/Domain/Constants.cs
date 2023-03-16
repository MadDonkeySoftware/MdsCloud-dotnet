namespace MdsCloud.CLI.Domain;

public static class Constants
{
    public const string SolutionFileName = "MdsCloud.All.sln";
    public const string SettingsFileName = "settings.json";
    public const string ConfigurationFileName = "config.json";

    internal static class StackSettingsKeys
    {
        public const string SourceDirectory = "sourceDirectory";
        public const string DefaultAdminPassword = "defaultAdminPassword";
    }

    internal static class StackConfiguration
    {
        internal static class Keys
        {
            public const string Identity = "identity";
            public const string NotificationService = "notification";
            public const string QueueService = "queue";
            public const string FileService = "file";
            public const string ServerlessFunctions = "serverlessFunctions";
            public const string StateMachine = "stateMachine";
        }

        internal static class Values
        {
            public const string Default = Stable;

            public const string Local = "local";
            public const string LocalDev = "localDev";
            public const string Latest = "latest";
            public const string Stable = "stable";
        }
    }

    internal static class Services
    {
        public const string Identity = "Identity";
        public const string NotificationService = "Notification";
        public const string QueueService = "Queue";
        public const string FileService = "File";
        public const string ServerlessFunctions = "Serverless Functions";
        public const string StateMachine = "State Machine";
    }

    internal static class DockerImageNames
    {
        public const string DbTooling = "mds-cloud-db-tooling";
        public const string Identity = "mds-cloud-identity";
        public const string NotificationService = "mds-notification-service";
        public const string QueueService = "mds-queue-service";
        public const string FileService = "mds-file-service";
        public const string ServerlessFunctions = "mds-serverless-functions";
        public const string StateMachine = "mds-state-machine";
    }
}
