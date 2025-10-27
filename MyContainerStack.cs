using System.Collections.Generic;
using Pulumi;
using AzureNative = Pulumi.AzureNative;

class MyCloudStack : Stack
{
    public MyCloudStack()
    {
        // 1. Create a Resource Group to hold all resources
        var resourceGroup = new AzureNative.Resources.ResourceGroup("resourceGroup");

        // 2. Define the Virtual Network and Subnet for the VM
        var virtualNetwork = new AzureNative.Network.VirtualNetwork("vnet", new AzureNative.Network.VirtualNetworkArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AddressSpace = new AzureNative.Network.Inputs.AddressSpaceArgs { AddressPrefixes = { "10.0.0.0/16" } },
            Subnets = new List<AzureNative.Network.Inputs.SubnetArgs>
            {
                new AzureNative.Network.Inputs.SubnetArgs { Name = "default", AddressPrefix = "10.0.1.0/24" }
            }
        });

        // 3. Create a Public IP Address
        var publicIp = new AzureNative.Network.PublicIPAddress("publicIp", new AzureNative.Network.PublicIPAddressArgs
        {
            ResourceGroupName = resourceGroup.Name,
            PublicIPAllocationMethod = AzureNative.Network.PublicIPAddressAllocationMethod.Static
        });

        // 4. Create a Network Interface (NIC) for the VM
        var networkInterface = new AzureNative.Network.NetworkInterface("nic", new AzureNative.Network.NetworkInterfaceArgs
        {
            ResourceGroupName = resourceGroup.Name,
            IpConfigurations = new List<AzureNative.Network.Inputs.NetworkInterfaceIPConfigurationArgs>
            {
                new AzureNative.Network.Inputs.NetworkInterfaceIPConfigurationArgs
                {
                    Name = "ipconfig1",
                    Subnet = new AzureNative.Network.Inputs.SubnetArgs { Id = virtualNetwork.Subnets.Apply(subnets => subnets[0].Id) },
                    PublicIPAddress = new AzureNative.Network.Inputs.PublicIPAddressArgs { Id = publicIp.Id }
                }
            }
        });

        // --- Important: Security Rule to allow HTTP traffic (Port 80) ---
        var nsg = new AzureNative.Network.NetworkSecurityGroup("nsg", new AzureNative.Network.NetworkSecurityGroupArgs
        {
            ResourceGroupName = resourceGroup.Name,
            SecurityRules = new List<AzureNative.Network.Inputs.SecurityRuleArgs>
            {
                new AzureNative.Network.Inputs.SecurityRuleArgs
                {
                    Name = "Allow-HTTP-Inbound",
                    Priority = 100,
                    Direction = AzureNative.Network.SecurityRuleDirection.Inbound,
                    Access = AzureNative.Network.SecurityRuleAccess.Allow,
                    Protocol = AzureNative.Network.SecurityRuleProtocol.Tcp,
                    SourcePortRange = "*",
                    DestinationPortRange = "80", // HTTP Port
                    SourceAddressPrefix = "Internet",
                    DestinationAddressPrefix = "*"
                }
            }
        });

        var nicNsgAssociation = new AzureNative.Network.NetworkInterfaceSecurityGroupAssociation("nicNsgAssociation", new AzureNative.Network.NetworkInterfaceSecurityGroupAssociationArgs
        {
            NetworkInterfaceId = networkInterface.Id,
            NetworkSecurityGroupId = nsg.Id,
        });

        // 5. Create the Virtual Machine
        var vm = new AzureNative.Compute.VirtualMachine("vm", new AzureNative.Compute.VirtualMachineArgs
        {
            ResourceGroupName = resourceGroup.Name,
            VmSize = "Standard_B1s", // Basic size for testing
            StorageProfile = new AzureNative.Compute.Inputs.StorageProfileArgs
            {
                ImageReference = new AzureNative.Compute.Inputs.ImageReferenceArgs // Using a recent Ubuntu LTS image
                {
                    Publisher = "Canonical",
                    Offer = "UbuntuServer",
                    Sku = "18.04-LTS",
                    Version = "latest",
                },
                OsDisk = new AzureNative.Compute.Inputs.OSDiskArgs
                {
                    CreateOption = "FromImage",
                    Name = "myosdisk",
                },
            },
            OsProfile = new AzureNative.Compute.Inputs.OSProfileArgs
            {
                ComputerName = "mycloudserver",
                AdminUsername = "pulumiuser",
                AdminPassword = "YourSecurePassword123!", // Replace with a real secret
                LinuxConfiguration = new AzureNative.Compute.Inputs.LinuxConfigurationArgs
                {
                    DisablePasswordAuthentication = false, // Set to true and use SSH keys for production
                },
            },
            HardwareProfile = new AzureNative.Compute.Inputs.HardwareProfileArgs
            {
                VmSize = "Standard_B1s",
            },
            NetworkProfile = new AzureNative.Compute.Inputs.NetworkProfileArgs
            {
                NetworkInterfaces = new List<AzureNative.Compute.Inputs.NetworkInterfaceReferenceArgs>
                {
                    new AzureNative.Compute.Inputs.NetworkInterfaceReferenceArgs { Id = networkInterface.Id, Primary = true }
                }
            }
        });

        // Export the Public IP Address of the server
        this.PublicIP = publicIp.IpAddress;
    }

    [Output]
    public Output<string> PublicIP { get; set; }
}
