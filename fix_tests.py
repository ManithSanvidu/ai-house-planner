with open('HousePlanner.API.Tests/Controllers/WorkflowControllerTests.cs', 'r') as f:
    content = f.read()

# Find the start of the tests I added and put them INSIDE the class
idx = content.find('public async Task ApprovedDesign_NoConstructorRequest_CanArchive()')
if idx != -1:
    idx = content.rfind('[Fact]', 0, idx)
    # Check where the class ends before idx
    class_end = content.rfind('}', 0, idx)
    if class_end != -1:
        # Move the tests inside the class
        tests = content[idx:]
        content = content[:idx]

        # Remove the extra } that closes the class prematurely
        # Or just append the tests properly
        pass

# A simpler way: I'll just restore the file from git, and then append properly.
import os
os.system("git checkout HousePlanner.API.Tests/Controllers/WorkflowControllerTests.cs")
