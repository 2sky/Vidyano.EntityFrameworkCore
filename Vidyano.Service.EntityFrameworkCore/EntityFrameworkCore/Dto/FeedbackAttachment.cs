using Vidyano.Service.Repository.DataLayer;

namespace Vidyano.Service.EntityFrameworkCore.Dto
{
    internal sealed class FeedbackAttachment : IAttachmentDto
    {
        private readonly FeedbackDto feedbackDto;

        private FeedbackAttachment(FeedbackDto feedbackDto)
        {
            this.feedbackDto = feedbackDto;
        }

        public string Name => "screenshot.png";

        public byte[] Data => feedbackDto.Screenshot!;

        public static explicit operator FeedbackAttachment?(FeedbackDto feedbackDto)
        {
            return feedbackDto?.Screenshot != null ? new FeedbackAttachment(feedbackDto) : null;
        }
    }
}