using UnityEngine;

namespace Xiangsoft.Game.Test
{
    public class TestPerson
    {
        public string Name { get; set; }
    }

    public class TypeTest : MonoBehaviour
    {
        private void Start()
        {
            TestPerson person = new TestPerson { Name = "张三" };

            string str = "你好啊";
            doSomethingString(str);
            doSomethingPerson(person);
            Debug.Log(str);
            Debug.Log(person.Name);
        }

        private void doSomethingString(string str)
        {
            str = "哈哈哈";
        }

        private void doSomethingPerson(TestPerson person)
        {
            person.Name = "李四";
        }
    }
}