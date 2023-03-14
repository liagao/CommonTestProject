// CplusplusTest.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <map>
#include <string>
using namespace std;

class Node
{
public:
    Node()
    {
        Next = NULL;
    }
    Node* Next;
};

void func(map<string, Node>& map1, map<string, Node*>& map2)
{
    string key = "123";
    Node node;
    map1[key] = node;
    map2[key] = &node;

    cout << "addr:" << &node << endl;
    cout << "addr in func:" << &map1[key] << endl;
}

int main()
{
    map<string, Node> map1;

    map<string, Node*> map2;

    func(map1, map2);

    cout << "src addr:"<< map2["123"]<<"\t"<<"dst addr:"<<&map1["123"]<<endl;

    //Node** result = new Node * [100];
    //GetMiddleNElement(NULL, 10, result);

    std::cout << "Hello World!\n";
}

enum SearchResult
{
    Success,
    Failed,
    PartiallySuccess
};

SearchResult GetMiddleNElement(Node* head, int n, Node**& result)
{
    result[1] = head;
    return SearchResult::Success;
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
