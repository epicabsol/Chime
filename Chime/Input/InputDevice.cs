using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Input
{
    public abstract class InputDevice
    {
        private abstract class InputEventBase
        {
            public abstract void Apply();
        }

        private class InputEvent<TValue> : InputEventBase where TValue : unmanaged
        {
            public InputElement<TValue>? Element { get; set; }
            public TValue Value { get; set; }
            public double ApplicationTime { get; set; }

            public override void Apply()
            {
                this.Element!.ChangeValue(this.Value, this.ApplicationTime, true);
            }
        }

        public abstract string DisplayName { get; }
        public abstract bool IsConnected { get; }
        public IReadOnlyList<InputElement<bool>> ActionElements { get; }
        public IReadOnlyList<InputElement<float>> AxisElements { get; }
        public IReadOnlyList<InputElement<Matrix4x4>> TransformElements { get; }

        public event EventHandler<EventArgs>? Removed;

        const int InitialPoolCapacity = 10;
        private Queue<InputEventBase> PendingEvents { get; } = new Queue<InputEventBase>();
        // Object pools for reusing the different InputEvent types
        private Queue<InputEvent<bool>> ActionEventPool { get; } = new Queue<InputEvent<bool>>();
        private Queue<InputEvent<float>> AxisEventPool { get; } = new Queue<InputEvent<float>>();
        private Queue<InputEvent<Matrix4x4>> TransformEventPool { get; } = new Queue<InputEvent<Matrix4x4>>();

        protected InputDevice(List<InputElement<bool>> actionElements, List<InputElement<float>> axisElements, List<InputElement<Matrix4x4>> transformElements)
        {
            this.ActionElements = actionElements.AsReadOnly();
            this.AxisElements = axisElements.AsReadOnly();
            this.TransformElements = transformElements.AsReadOnly();

            for (int i = 0; i < InputDevice.InitialPoolCapacity; i++)
            {
                this.ActionEventPool.Enqueue(new InputEvent<bool>());
                this.AxisEventPool.Enqueue(new InputEvent<float>());
                this.TransformEventPool.Enqueue(new InputEvent<Matrix4x4>());
            }
        }

        private void QueueInputEvent<TValue>(InputElement<TValue> element, TValue newValue, double applicationTime, Queue<InputEvent<TValue>> eventPool) where TValue : unmanaged
        {
            InputEvent<TValue> inputEvent;
            if (!eventPool.TryDequeue(out inputEvent!))
            {
                inputEvent = new InputEvent<TValue>();
            }

            inputEvent.Element = element;
            inputEvent.Value = newValue;
            inputEvent.ApplicationTime = applicationTime;

            this.PendingEvents.Enqueue(inputEvent);
        }

        protected void QueueActionEvent(InputElement<bool> element, bool newValue, double applicationTime)
        {
            this.QueueInputEvent(element, newValue, applicationTime, this.ActionEventPool);
        }

        protected void QueueAxisEvent(InputElement<float> element, float newValue, double applicationTime)
        {
            this.QueueInputEvent(element, newValue, applicationTime, this.AxisEventPool);
        }

        protected void QueueTransformEvent(InputElement<Matrix4x4> element, Matrix4x4 newValue, double applicationTime)
        {
            this.QueueInputEvent(element, newValue, applicationTime, this.TransformEventPool);
        }

        /// <summary>
        /// Processes all the input events that have been received for this device, updating the <see cref="InputElement{TValue}.Value"/> of the <see cref="InputElement{TValue}"/>s and causing their <see cref="InputElement{TValue}.ValueChanged"/> events to fire.
        /// </summary>
        public void ProcessEvents()
        {
            InputEventBase inputEvent;
            while (this.PendingEvents.TryDequeue(out inputEvent!))
            {
                inputEvent.Apply();

                // If the event is one of the recognized types, put it back in the respective object pool
                if (inputEvent is InputEvent<bool> actionEvent)
                {
                    this.ActionEventPool.Enqueue(actionEvent);
                }
                else if (inputEvent is InputEvent<float> axisEvent)
                {
                    this.AxisEventPool.Enqueue(axisEvent);
                }
                else if (inputEvent is InputEvent<Matrix4x4> transformEvent)
                {
                    this.TransformEventPool.Enqueue(transformEvent);
                }
            }
        }

        public void Remove()
        {
            this.Removed?.Invoke(this, new EventArgs());
        }
    }

    public abstract class InputElementBase
    {
        public abstract string DisplayName { get; }
    }

    public class InputEventArgs<TValue> : EventArgs where TValue : unmanaged
    {
        public InputElement<TValue> Element { get; }
        public TValue NewValue { get; }
        public TValue OldValue { get; }
        public double ApplicationTime { get; }

        public InputEventArgs(InputElement<TValue> element, TValue newValue, TValue oldValue, double applicationTime)
        {
            this.Element = element;
            this.NewValue = newValue;
            this.OldValue = oldValue;
            this.ApplicationTime = applicationTime;
        }
    }

    public class InputElement<TValue> : InputElementBase where TValue : unmanaged
    {
        private TValue _value;
        public TValue Value => this._value;

        public override string DisplayName { get; }

        public event EventHandler<InputEventArgs<TValue>>? ValueChanged;

        public InputElement(string displayName)
        {
            this.DisplayName = displayName;
        }

        internal void ChangeValue(TValue newValue, double applicationTime, bool fireEvent = true)
        {
            TValue oldValue = this.Value;
            this._value = newValue;
            if (fireEvent)
            {
                this.ValueChanged?.Invoke(this, new InputEventArgs<TValue>(this, newValue, oldValue, applicationTime));
            }
        }
    }
}
