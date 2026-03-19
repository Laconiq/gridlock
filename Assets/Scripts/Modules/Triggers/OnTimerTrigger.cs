namespace AIWE.Modules.Triggers
{
    public class OnTimerTrigger : TriggerInstance
    {
        private readonly float _interval;
        private float _timer;

        public OnTimerTrigger(float interval)
        {
            _interval = interval;
        }

        public override void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer >= _interval)
            {
                _timer = 0f;
                Fire();
            }
        }

        public override void Reset()
        {
            base.Reset();
            _timer = 0f;
        }
    }
}
