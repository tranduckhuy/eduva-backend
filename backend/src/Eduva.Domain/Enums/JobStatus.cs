namespace Eduva.Domain.Enums
{
    /// <summary>
    /// Job processing status
    /// </summary>
    public enum JobStatus
    {
        /// <summary>
        /// Job is queued and being processed
        /// </summary>
        Processing = 1,

        /// <summary>
        /// AI content has been generated, awaiting review
        /// </summary>
        ContentGenerated = 2,

        /// <summary>
        /// Creating final product (video/audio file)
        /// </summary>
        CreatingProduct = 3,

        /// <summary>
        /// Job completed successfully
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Job failed during processing
        /// </summary>
        Failed = 5,

        /// <summary>
        /// Job expired due to timeout
        /// </summary>
        Expired = 6,

        /// <summary>
        /// Job was cancelled by user
        /// </summary>
        Cancelled = 7
    }
}
