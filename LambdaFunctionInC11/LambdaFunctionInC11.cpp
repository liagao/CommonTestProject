// LambdaFunctionInC11.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include "LambdaFunctionInC11.h"

void SelfCleanVector::Insert(const char* p_str, const boost::function<void(const char*)>& p_cleanupCallback)
{
    p_cleanupCallback(p_str);
}

int main()
{
    SelfCleanVector* m_selfCleanVector = new SelfCleanVector();
    // Add to cleanup list.
    const char *str = "Hello World";

    int ss = 123;
    m_selfCleanVector->Insert(str,
        [ss, &str](const char* p_str) mutable
        {
			//delete p_str;
            ss = 456;
		}
    );
    std::cout << "Hello World!\n"<< ss;

    /*int a = 1, b = 1, c = 1;

    auto m1 = [a, &b, &c]() mutable
        {
            auto m2 = [a, b, &c]() mutable
                {
                    std::cout << a << b << c << '\n';
                    a = 4; b = 4; c = 4;
                };
            a = 3; b = 3; c = 3;
            m2();
        };

    a = 2; b = 2; c = 2;

    m1();                             // calls m2() and prints 123
    std::cout << a << b << c << '\n'; // prints 234*/
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
